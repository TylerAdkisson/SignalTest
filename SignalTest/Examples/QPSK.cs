using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalTest.Examples
{
    class QPSK
    {
        static readonly string LocalPath = Path.Combine(Program.RootDir, "QPSK");

        public static void OffsetQPSK()
        {
            Init();

            ModulateOffsetQPSK();
            DemodulateOffsetQPSK();
        }

        public static void NormalQPSK()
        {
            Init();

            ModulateQPSK();
            DemodulateQPSK();
        }

        static void Init()
        {
            Directory.CreateDirectory(LocalPath);
        }

        static void ModulateQPSK()
        {
            // Manual adjust values
            int sampleRate = 8000;
            int symbolCount = 1000;
            float symbolRate = 250; // Symbols per second
            int carrierHz = 1000;
            string outputPath = Path.Combine(LocalPath, "qpsk_output.8k.3ch.pcm32f");

            // Fixed values
            float symbolPeriod = 1f / symbolRate;
            int symbolLengthSamples = sampleRate / (int)symbolRate;
            Vco vco = new Vco(sampleRate, carrierHz);
            PseudoRandom random = new PseudoRandom(23);

            using (Stream output = File.Create(outputPath))
            {
                for (int i = 0; i < symbolCount; i++)
                {
                    // Generate 2 data bits
                    int bits = (int)random.Next(0, 4);

                    // Generate symbol samples
                    for (int p = 0; p < symbolLengthSamples; p++)
                    {
                        // Generate symbol sample
                        float sampleI = (float)vco.Cos() * ((bits >> 1) * 2 - 1);
                        float sampleQ = (float)vco.Sin() * ((bits & 1) * 2 - 1);

                        vco.Next();

                        // Reduce amplitude so the sum equals 1.0
                        sampleI *= 0.707f;
                        sampleQ *= 0.707f;

                        // Combine I and Q samples
                        float sampleOutput = sampleI + sampleQ;

                        // Output sample to file
                        output.Write(BitConverter.GetBytes(sampleOutput), 0, 4);
                        output.Write(BitConverter.GetBytes(((bits >> 1) * 2f - 1)), 0, 4);
                        output.Write(BitConverter.GetBytes(((bits & 1) * 2f - 1)), 0, 4);
                    }
                }
            }
        }

        static void ModulateOffsetQPSK()
        {
            // Manual adjust values
            int sampleRate = 8000;
            int symbolCount = 1000;
            float symbolRate = 250; // Symbols per second
            int carrierHz = 1000;
            string outputPath = Path.Combine(LocalPath, "oqpsk_output.8k.3ch.pcm32f");

            // Fixed values
            float symbolPeriod = 1f / symbolRate;
            int symbolLengthSamples = sampleRate / (int)symbolRate;
            Vco vco = new Vco(sampleRate, carrierHz);
            PseudoRandom random = new PseudoRandom(23);

            BiQuadraticFilter iFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, symbolRate, sampleRate, 0.707);
            BiQuadraticFilter qFilter = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, symbolRate, sampleRate, 0.707);

            using (Stream output = File.Create(outputPath))
            {
                int bits = 0;
                int nextBits = 0;
                for (int i = 0; i < symbolCount * 2; i++)
                {
                    bool even = i % 2 == 0;

                    // On each half-symbol period, shift in one data bit for transmission
                    //   while keeping the previous other arm's bit transmitting
                    if (even)
                    {
                        nextBits = (int)random.Next(0, 4);
                        bits = (bits & 0x01) | (nextBits & 0x02);
                    }
                    else
                    {
                        bits = (bits & 0x02) | (nextBits & 0x01);
                    }

                    // Generate symbol samples
                    for (int p = 0; p < symbolLengthSamples / 2; p++)
                    {
                        // Generate symbol sample
                        // Also filter symbols with a low pass filter for basic pulse shaping
                        float sampleI = (float)vco.Cos() * (float)iFilter.filter((bits >> 1) * 2 - 1);
                        float sampleQ = (float)vco.Sin() * (float)qFilter.filter((bits & 1) * 2 - 1);

                        vco.Next();

                        // Reduce amplitude so the sum equals 1.0
                        sampleI *= 0.707f;
                        sampleQ *= 0.707f;

                        // Combine I and Q samples
                        float sampleOutput = sampleI + sampleQ;

                        // Output sample to file
                        output.Write(BitConverter.GetBytes(sampleOutput), 0, sizeof(float));
                        output.Write(BitConverter.GetBytes(((bits >> 1) * 2f - 1)), 0, sizeof(float));
                        output.Write(BitConverter.GetBytes(((bits & 1) * 2f - 1)), 0, sizeof(float));
                    }
                }
            }
        }

        static void DemodulateQPSK()
        {
            // Manual adjust values
            int sampleRate = 8000;
            float symbolRate = 250; // Symbols per second
            int carrierHz = 1005;
            string inputPath = Path.Combine(LocalPath, "qpsk_output.8k.3ch.pcm32f");
            string outputPath = Path.Combine(LocalPath, "qpsk_output_demodulate.8k.2ch.pcm32f");

            // Fixed values
            float symbolPeriod = 1f / symbolRate;
            int symbolLengthSamples = sampleRate / (int)symbolRate;
            Costas costas = new Costas(sampleRate, carrierHz, Costas.LoopType.QPSK);
            Gardner gardner = new Gardner();
            Downsampler dsI = new Downsampler(sampleRate);
            Downsampler dsQ = new Downsampler(sampleRate);
            dsI.SetRatio((symbolRate * 2) / sampleRate);
            dsQ.SetRatio((symbolRate * 2) / sampleRate);
            Integrator intRatio = new Integrator(0.3f, (1f / (symbolRate * 2)) * 0.5f, (symbolRate * 2) / sampleRate);

            // Tweakable parameters
            costas.ArmFilterHz = symbolRate * 1.5f;

            using (Stream input = File.OpenRead(inputPath))
            {
                // Data input buffer
                byte[] buffer = new byte[sizeof(float) * 3];
                bool flipFlop = false;
                
                using (Stream output = File.Create(outputPath))
                {
                    int bytesRead = 0;
                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) == buffer.Length)
                    {
                        // Get input sample as a float
                        float sampleInput = BitConverter.ToSingle(buffer, 0);

                        // Process sample with costas loop
                        costas.Process(sampleInput);

                        float sampleI = costas.Inphase();
                        float sampleQ = costas.Quadrature();

                        // Downsample to 2x symbol rate for timing recovery
                        dsQ.Next();
                        if (dsI.Next())
                        {
                            // Compute timing error
                            float error = gardner.Process(dsI.GetSample(), dsQ.GetSample());
                            float ratio = intRatio.Process(error * 0.01f);// ((symbolRate * 16) / sampleRate) + (0.7f * error);

                            // Adjust ratio
                            dsI.SetRatio(ratio);
                            dsQ.SetRatio(ratio);

                            // Output every other sample
                            if (flipFlop ^= true)
                            {
                                // Output timing bits to file
                                output.Write(BitConverter.GetBytes(dsI.GetSample()), 0, sizeof(float));
                                output.Write(BitConverter.GetBytes(dsQ.GetSample()), 0, sizeof(float));
                            }
                        }

                        dsI.SupplyInput(sampleI);
                        dsQ.SupplyInput(sampleQ);

                        //// Output sample to file
                        //output.Write(BitConverter.GetBytes(sampleI), 0, sizeof(float));
                        //output.Write(BitConverter.GetBytes(sampleQ), 0, sizeof(float));
                    }
                }
            }
        }

        static void DemodulateOffsetQPSK()
        {
            // Manual adjust values
            int sampleRate = 8000;
            float symbolRate = 250; // Symbols per second
            int carrierHz = 1005;
            string inputPath = Path.Combine(LocalPath, "oqpsk_output.8k.3ch.pcm32f");
            string outputPath = Path.Combine(LocalPath, "oqpsk_output_demodulate.8k.2ch.pcm32f");
            int inputChannelCount = 3;

            // Fixed values
            float symbolPeriod = 1f / symbolRate;
            int symbolLengthSamples = sampleRate / (int)symbolRate;
            Costas costas = new Costas(sampleRate, carrierHz, Costas.LoopType.QPSK);

            // Downsamplers for bit slicing
            Downsampler dsI = new Downsampler(sampleRate);
            Downsampler dsQ = new Downsampler(sampleRate);
            dsI.SetRatio((symbolRate * 2) / sampleRate);
            dsQ.SetRatio((symbolRate * 2) / sampleRate);

            // Integrator to track symbol rate
            Integrator intRatio = new Integrator(0.3f, (1f / (symbolRate * 2)) * 0.5f, (symbolRate * 2) / sampleRate);

            // Modified gardner timing error detector
            Gardner gardner = new Gardner();


            // Tweakable parameters
            costas.ArmFilterHz = symbolRate * 1.5f;

            using (Stream input = File.OpenRead(inputPath))
            {
                using (Stream output = File.Create(outputPath))
                {
                    // Data input buffer
                    byte[] buffer = new byte[sizeof(float) * inputChannelCount];
                    bool flipFlop = false;
                    float sampleBufferI = 0;

                    int bytesRead = 0;
                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) == buffer.Length)
                    {
                        // Get input sample as a float
                        float sampleInput = BitConverter.ToSingle(buffer, 0);

                        // Process sample with costas loop
                        costas.Process(sampleInput);

                        // Get samples for I and Q channels
                        float sampleI = costas.Inphase();
                        float sampleQ = costas.Quadrature();

                        // Downsample to 2x symbol rate for timing recovery
                        dsQ.Next();
                        if (dsI.Next())
                        {
                            // Compute timing error
                            float error = gardner.Process(sampleBufferI);
                            float ratio = intRatio.Process(error * 0.1f);

                            // Adjust ratio
                            dsI.SetRatio(ratio);
                            dsQ.SetRatio(ratio);

                            // Output every other sample
                            if (flipFlop ^= true)
                            {
                                // Output timing bits to file
                                output.Write(BitConverter.GetBytes(sampleBufferI), 0, sizeof(float));
                                output.Write(BitConverter.GetBytes(dsQ.GetSample()), 0, sizeof(float));
                            }

                            // Buffer the I channel by half a symbol period because we delayed Q during modulation
                            // This puts I and Q back in sync resulting in our original data stream
                            // NOTE: This assumption only holds if the demodulated phase is correct and the
                            //   constellation is not rotated the wrong way around
                            sampleBufferI = dsI.GetSample();
                        }

                        // Supply data to downsamplers
                        dsI.SupplyInput(sampleI);
                        dsQ.SupplyInput(sampleQ);

                        //// Output full-rate samples to file instead
                        //output.Write(BitConverter.GetBytes(sampleI), 0, sizeof(float));
                        //output.Write(BitConverter.GetBytes(sampleQ), 0, sizeof(float));
                    }
                }
            }
        }
    }
}
