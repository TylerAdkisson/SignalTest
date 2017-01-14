using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SignalTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //SineTest();
            //Costas();
            //PllTest1();
            //FMModulate();

            GenerateQPSK();

            AWGN();


            // Tests for modularized objects
            //Costas3();
            //TimingRecovery();

            DecisionDirected();


            Const();
        }

        static void SineTest()
        {
            Random r = new Random(7);
            Osc osc = Osc.FromFrequency(1000, 50);

            // TODO: Make sub frequency be x4, instead of base being x1 and sub being /4
            Osc oscRef = Osc.FromFrequency(4005, 50);
            oscRef.SetDivisor(4);
            //Osc oscRef2 = Osc.FromFrequency(1000, 100);

            //Osc2 osc = new Osc2(1000, 44100);
            //Osc2 oscRef = new Osc2(1060, 44100);

            BiQuadraticFilter bqf1 = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 50, 44100, 0.707);
            //BiQuadraticFilter bqf2 = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 10, 44100, 0.707);
            BiQuadraticFilter bqf3 = new BiQuadraticFilter(BiQuadraticFilter.Type.BANDPASS, 1000, 44100, 10);
            BiQuadraticFilter bqf4A = new BiQuadraticFilter(BiQuadraticFilter.Type.NOTCH, 2000, 44100, 10);
            BiQuadraticFilter bqf4B = new BiQuadraticFilter(BiQuadraticFilter.Type.HIGHPASS, 4000, 44100, 10);

            using (Stream fs = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_1.pcm32f")))
            {

                long sampleCount = 44100 * 2;
                byte[] sampleBuffer = new byte[4];
                using (Stream fsIn = File.OpenRead("G:\\qpsk31_1khz.44k1.1ch.pcm32f"))
                {
                    sampleCount = fsIn.Length / 4;
                    //double adjHz = 200.0 / (44100.0 * 2.0);

                    //// Offset phase
                    //for (int i = 0; i < 5512 / 2; i++)
                    //{
                    //    osc.step();
                    //}

                    int fRef = 0;
                    int fSig = 0;

                    float lastS = 0;
                    float lastS2 = 0;

                    //float lastErr = 0;
                    float gain = 1f;
                    //float charge = 0f;
                    //float lastFilter = 0f;

                    double twopi = 2 * Math.PI;
                    double timeDelta = 1.0 / 44100;
                    float alpha = (float)((twopi * timeDelta * 300) / (twopi * timeDelta * 300 + 1));

                    //float prop = 1f / 128f;
                    //float deriv = 64;
                    //float lastErr = 0f;

                    //float pll_integral = 0f;
                    //float last_int = 0f;

                    float hzPerSample = 10f / 44100f;
                    int mode = 0;

                    // PI filter
                    float pTerm = 0f;
                    float iState = 0f;
                    float iTerm = 0f;

                    float pGain = 1f;
                    float iGain = (1f / 44100f) * 30f; //0.001f;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        fsIn.Read(sampleBuffer, 0, 4);
                        if (i < 58000)
                            continue;

                        if (i == 10000)
                        {
                            Debugger.Break();
                        }
                        //osc.AdjustHz(-adjHz);

                        osc.Next();
                        oscRef.Next();

                        float s = 0.5f * (float)osc.Sqr(); // /*0.126f * */(float)Sin2Square(osc.sin());
                        float s2 = (float)oscRef.Cos(); /*(float)Sin2Square(oscRef.Sin());*/ ///*0.126f * */(float)Sin2Square(oscRef.cos());

                        //s += (float)(r.NextDouble() * 2.0 - 1.0) * 0.6f;
                        //s = (float)bqf3.filter(s);
                        //s *= 3f;
                        //s = (float)Clip(s, 1.0);


                        float s12 = s = BitConverter.ToSingle(sampleBuffer, 0);

                        // Add noise
                        //s12 = s += (float)(r.NextDouble() * 2.0 - 1.0) * 3f;
                        //s12 = s *= 0.33f;

                        s = (float)bqf3.filter(s);

                        // Raise to ^4 to remove QPSK modulation
                        s = s * s * s * s;
                        // High-pass and notch filters
                        s = (float)bqf4B.filter(s);
                        //s = (float)bqf4A.filter(s);
                        s *= 3f;
                        s = (float)Clip(s, 1.0);

                        // Convert to square
                        //s *= 40f;
                        //s = (float)Clip(s, 1.0);
                        //s = (float)Sin2Square(s);

                        //float s6 = (float)Sin2Square(Math.Cos(2 * Math.PI * 1020f * (i / 44100f + pll_integral)));
                        //s2 = s6;

                        float s3 = 0f;// (float)bqf1.filter(s * s2);

                        //// Effectively XOR
                        //if ((s > 0 || s2 > 0) && !(s > 0 && s2 > 0))
                        //    s3 = 1;
                        //else
                        //    s3 = 0;

                        // Simulate D flip-flops for phase-frequency detector
                        if (fSig == 1 && fRef == 1)
                            fRef = fSig = 0;

                        if (lastS <= 0.063 && s > 0.063)
                            fSig = 1;
                        if (lastS2 <= 0.063 && s2 > 0.063)
                            fRef = 1;


                        s3 = -(fRef - fSig);
                        s3 = s * s2;


                        lastS = s;
                        lastS2 = s2;

                        float s4 = 0;///*s3 + (s3 - lastErr) * 0.01f;*/ 
                        s4 = (float)bqf1.filter(s3 * gain);

                        //float errDiff = (float)bqf2.filter(s4 - lastErr);
                        //if ((mode == 0 && i > 1000 && Math.Abs(errDiff) < 0.00001f))// || (mode == 1 && i - last_int >= 10000))
                        //{
                        //    // Switch to phase correction
                        //    oscRef.baseFq = oscRef.fq;
                        //    //bqf1.reset();
                        //    //bqf1.reconfigure(10);
                        //    gain = 0.25f;

                        //    mode = 1;
                        //    //last_int = i;
                        //}
                        //else if (mode == 1 && Math.Abs(errDiff) > 0.0001f)
                        //{
                        //    gain = 0.5f;
                        //    mode = 0;
                        //}
                        //lastErr = s4;


                        //s4 = lastFilter + alpha * ((s3 * 0.25f) - lastFilter);
                        //lastFilter = s4;
                        //if (lastFilter > 1f)
                        //    lastFilter = 1f;
                        //if (lastFilter < -1f)
                        //    lastFilter = -1f;
                        //s4 = lastFilter;

                        //if (s3 != 0)
                        //Debugger.Break();

                        //float s5 = lastFilter + alpha * (s4 + lastErr); //lastFilter + (1f / 44100f) * 1.0f * (s4 + lastErr);
                        //lastErr = s4;
                        //lastFilter = s5;


                        //lastFilter += s4 / 44100f;
                        //if (lastFilter != 0)
                        //{
                        //    //Debugger.Break();
                        //}
                        //oscRef.fq = oscRef.baseFq + lastFilter;

                        //if (pll_integral != 0)
                        //{
                        //    //Debugger.Break();
                        //}

                        //pll_integral += (s4) / (44100f);


                        ////oscRef.Phase(pll_integral);
                        ////oscRef._fq += s4 / 441000f;
                        //last_int = pll_integral;
                        //float s5 = pll_integral;// lastFilter;


                        // Run PI filter
                        pTerm = s4 * pGain;

                        iState += s4;
                        iTerm = iGain * iState;

                        float s5 = pTerm + iTerm;



                        //oscRef.fq = oscRef.fq + (s4 * (1f / 44100f));
                        //oscRef.ContainSpan();
                        if ((i % 100) == 0)
                        {
                            Console.WriteLine("{0:F2} {1:F2}", oscRef.FqHz, oscRef.SubFqHz);
                        }

                        //float s5 = (float)bqf2.filter(s4);
                        //float s5 = (float)bqf2.filter(s4 - lastFilter);
                        //lastFilter = s4;

                        //float f1 = lastFilter + alpha * (s3 - lastFilter);
                        //lastFilter = f1;
                        //Console.WriteLine(f1);

                        //lastFilter += s3 * alpha;
                        //if (lastFilter > 1)
                        //    lastFilter = 1;
                        //if (lastFilter < -1)
                        //    lastFilter = -1;
                        //float f1 = lastFilter;

                        //double spanHz = 50.0;

                        //double newHz = 1020 + (spanHz * s4);

                        //oscRef.fq += (alpha * s4);
                        //oscRef.ContainSpan();
                        //oscRef.AdjustPercent(s3 / 100000f);
                        oscRef.SetFrequency(s5);
                        float s10 = (float)oscRef.SinSub();
                        float s11 = (float)oscRef.CosSub();

                        //s4 = (float)(oscRef.fq / oscRef.baseFq) - 1f;

                        //Console.WriteLine("{0:F0}", oscRef.FqHz);
                        //gain *= 0.999f;

                        //Console.WriteLine(s * s2);

                        //s = (float)osc.sin();
                        //s2 = (float)oscRef.sin();


                        fs.Write(BitConverter.GetBytes(s12), 0, 4);
                        fs.Write(BitConverter.GetBytes(s2), 0, 4);
                        fs.Write(BitConverter.GetBytes(s3), 0, 4);
                        fs.Write(BitConverter.GetBytes(s4), 0, 4);
                        fs.Write(BitConverter.GetBytes(s5), 0, 4);

                        fs.Write(BitConverter.GetBytes(s10), 0, 4);
                        fs.Write(BitConverter.GetBytes(s11), 0, 4);
                    }
                }
            }
        }

        static void Costas()
        {
            Random r = new Random();
            Osc osc = Osc.FromFrequency(1000, 50);
            Osc oscRef = Osc.FromFrequency(4030, 50);

            //Osc2 osc = new Osc2(1000, 44100);
            //Osc2 oscRef = new Osc2(1060, 44100);


            BiQuadraticFilter f1A = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 90, 44100, 0.707);
            BiQuadraticFilter f1B = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 90, 44100, 0.707);
            BiQuadraticFilter f2A = new BiQuadraticFilter(BiQuadraticFilter.Type.NOTCH, 2000, 44100, 0.707);
            BiQuadraticFilter f2B = new BiQuadraticFilter(BiQuadraticFilter.Type.HIGHPASS, 4000, 44100, 0.707);
            BiQuadraticFilter fAGC = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 1, 44100, 0.707);
            //BiQuadraticFilter f3B = new BiQuadraticFilter(BiQuadraticFilter.Type.HIGHPASS, 4000, 44100, 0.707);

            for (int i = 0; i < 100; i++)
            {
                fAGC.filter(1.0);
            }


            using (Stream fs = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_1.pcm32f")))
            {

                //// Offset phase
                //for (int i = 0; i < 5512 / 3; i++)
                //{
                //    osc.Step();
                //}

                float integral = 0f;
                long sampleCount = 44100 * 2;
                byte[] sampleBuffer = new byte[4];

                float pTerm = 0f;
                float iState = 0f;
                float iTerm = 0f;

                float pGain = 50f;
                float iGain =  (1f / 44100f) * 300f; //0.001f;

                float avgAmp = 1f;

                using (Stream fsIn = File.OpenRead(/*"G:\\bpsk31_1000hz.44.1.pcm32f"*/"g:\\qpsk31_1khz.44k1.1ch.pcm32f"))
                {
                    sampleCount = fsIn.Length / 4;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        fsIn.Read(sampleBuffer, 0, 4);
                        //if (i < 58000)
                        //    continue;

                        if (i == 10000)
                        {
                            Debugger.Break();
                        }
                        //osc.AdjustHz(-adjHz);

                        osc.Next();
                        oscRef.Next();

                        // Input signal
                        float s = 0f;// 0.25f * (float)osc.Sin();
                        s = BitConverter.ToSingle(sampleBuffer, 0);

                        s = s * s * s * s;
                        s = (float)f2A.filter(f2B.filter(s));

                        avgAmp = (float)fAGC.filter(Math.Abs(s));
                        //Console.WriteLine(avgAmp);
                        s *= 0.07f / avgAmp;


                        //s += (float)(r.NextDouble() * 2.0 - 1.0) * 0.5f;
                        //s *= 4f;

                        //s = (float)f2.filter(s) * 4f;

                        // Generate refs
                        float s2A = 1f * (float)-oscRef.Sin();
                        float s2B = 1f * (float)oscRef.Cos();


                        // Phase detect
                        float s3A = s2A * s;
                        float s3B = s2B * s;

                        //s3A *= 0.33f;
                        //s3B *= 0.33f;

                        // Filter
                        float s4A = (float)f1A.filter(s3A);
                        float s4B = (float)f1B.filter(s3B);

                        float s4A1 = s4A;// * (float)Sign(s4B);
                        float s4B1 = s4B;// * (float)Sign(s4A);

                        float s6 = s4A1 * s4B1;
                        //s5 = (float)f2.filter(s5);

                        // Run PI filter
                        pTerm = s6 * pGain;

                        iState += s6;
                        iTerm = iGain * iState;
                        
                        float s5 = pTerm + iTerm;

                        oscRef.SetFrequency(s5);
                        if (i % 100 == 0)
                        Console.WriteLine(oscRef.FqHz);

                        fs.Write(BitConverter.GetBytes(s), 0, 4);
                        //fs.Write(BitConverter.GetBytes(s2A), 0, 4);
                        fs.Write(BitConverter.GetBytes(s2B), 0, 4);

                        //fs.Write(BitConverter.GetBytes((float)Sign(s4A)), 0, 4);
                        //fs.Write(BitConverter.GetBytes((float)Sign(s4B)), 0, 4);

                        fs.Write(BitConverter.GetBytes(s4A), 0, 4);
                        fs.Write(BitConverter.GetBytes(s4B), 0, 4);

                        fs.Write(BitConverter.GetBytes(s5), 0, 4);
                        fs.Write(BitConverter.GetBytes(avgAmp), 0, 4);
                    }
                }
            }
        }

        static void PllTest1()
        {
            Random r = new Random(7);
            Vco vcoRef = new Vco(44100, 1000, 100);
            int fourTimesIndex = vcoRef.AddMultiplier(4);
            //vcoRef.SetPhaseOffset(0, -(Math.PI * 10 / 180.0));


            // Filters
            BiQuadraticFilter fArm1 = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 90, 44100, 0.707);
            BiQuadraticFilter fFourTimesHP4x = new BiQuadraticFilter(BiQuadraticFilter.Type.HIGHPASS, 2000, 44100, 0.707);
            BiQuadraticFilter fFourTimesHP2x = new BiQuadraticFilter(BiQuadraticFilter.Type.HIGHPASS, 1000, 44100, 0.707);
            BiQuadraticFilter fFourTimesN = new BiQuadraticFilter(BiQuadraticFilter.Type.NOTCH, 2000, 44100, 10);
            BiQuadraticFilter fFourTimesN2 = new BiQuadraticFilter(BiQuadraticFilter.Type.NOTCH, 2000, 44100, 10);
            BiQuadraticFilter fFourTimesN3 = new BiQuadraticFilter(BiQuadraticFilter.Type.NOTCH, 2000, 44100, 10);
            //BiQuadraticFilter fFourTimesBand = new BiQuadraticFilter(BiQuadraticFilter.Type.BANDPASS, 4000, 44100, 20);
            BiQuadraticFilter fErr1 = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 5, 44100, 0.707);
            //BiQuadraticFilter fBandpass = new BiQuadraticFilter(BiQuadraticFilter.Type.BANDPASS, 1000, 44100, 20);
            //BiQuadraticFilter fVco = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 60, 44100, 0.707);

            BiQuadraticFilter fDataI = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 90, 44100, 0.707);
            BiQuadraticFilter fDataQ = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 90, 44100, 0.707);

            //BiQuadraticFilter fPeak1 = new BiQuadraticFilter(BiQuadraticFilter.Type.PEAK, 4000, 44100, 10, 36);



            // PI filter
            float pTerm = 0f;
            float iState = 0f;
            float iTerm = 0f;

            float pGain = 1f;
            float iGain = (1f / 44100f) * 30f; //0.001f;

            float lastErr = 0f;
            int lockedSamples = 0;

            int fRef = 0;
            int fSig = 0;

            float lastS = 0;
            float lastS2 = 0;

            using (Stream fs = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_1.pcm32f")))
            {
                long sampleCount = 44100 * 2;
                byte[] sampleBuffer = new byte[4];
                string inputFile = "";
                inputFile = "G:\\qpsk31_1khz.44k1.1ch.pcm32f";
                //inputFile = "G:/qpsk_real_microphone_filtered.44k1.1ch.pcm32f";
                //inputFile = "G:/TestVoice3_fm2_44k_1ch.pcm32s";
                //inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_2_FM.pcm32f");
                //inputFile = "G:/rtty45.44k1.1ch.pcm32f";
                //inputFile = "G:/sstv_bs12sec.pcm32f";
                //inputFile = "G:/4xCarrier.44k1.1ch.pcm32f";

                using (Stream fsIn = File.OpenRead(inputFile))
                {
                    sampleCount = fsIn.Length / 4;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        fsIn.Read(sampleBuffer, 0, 4);
                        //if (i < 28000)
                        //    continue;

                        // Advance oscillator
                        vcoRef.Next();


                        float sRef = (float)vcoRef.Cos(fourTimesIndex);
                        float sInput = BitConverter.ToSingle(sampleBuffer, 0);
                        float sOriginalInput = sInput;

                        //sInput = (float)fBandpass.filter(sInput) * 2f;
                        //sInput = (float)Clip(sInput, 0.5);
                        //sInput *= 2f;

                        //sInput += (float)(r.NextDouble() * 2.0 - 1.0) * 0.126f;


                        // Raise to ^4 to remove QPSK modulation
                        sInput = sInput * sInput;
                        sInput = (float)fFourTimesHP2x.filter(sInput);
                        //sInput *= 5.98f; // 18 dB increase

                        sInput = sInput * sInput;
                        sInput = (float)fFourTimesHP4x.filter(sInput);

                        // High-pass and notch filters
                        //sInput = (float)fFourTimesHP.filter(sInput);
                        //sInput = (float)fFourTimesBand.filter(sInput);
                        //sInput = (float)fFourTimesN.filter(sInput);
                        //sInput = (float)fFourTimesN2.filter(sInput);
                        //sInput = (float)fFourTimesN3.filter(sInput);
                        //sInput *= 1.5f; // 3 dB increase
                        //sInput *= 2f; // 6 dB increase
                        sInput *= 3.98f; // 12 dB increase
                        sInput *= 15.8f; // 24 dB increase
                        //sInput *= 63f; // 36 dB increase

                        //sInput = (float)fFourTimesBand.filter(sInput);
                        //sInput *= 63f;
                        //sInput *= 0.0158f;
                        //sInput *= 0.5f;

                        //sInput = (float)Clip(sInput, 1.0);

                        // Phase detection
                        float sPhase = sInput * sRef;

                        //// Simulate D flip-flops for phase-frequency detector
                        //if (fSig == 1 && fRef == 1)
                        //    fRef = fSig = 0;

                        //if (lastS < 0.063 && sInput > 0.063)
                        //    fSig = 1;
                        //if (lastS2 < 0.063 && sRef > 0.063)
                        //    fRef = 1;


                        //sPhase = -(fRef - fSig);

                        sPhase = (float)fArm1.filter(sPhase);
                        //sPhase = (float)Math.Tanh(sPhase);


                        // Run PI filter (integrator)
                        pTerm = sPhase * pGain;

                        iState += sPhase;
                        iTerm = iGain * iState;
                        if (iState * iGain > 1f)
                            iState = 1f / iGain;
                        else if (iState * iGain < -1f)
                            iState = -1f / iGain;

                        float sVcoControl = pTerm + iTerm;
                        //sVcoControl = (float)fVco.filter(sVcoControl);

                        // Multiplying by 100 ~= 40dB increase
                        float sErrTrend = (float)fErr1.filter(Math.Abs(sVcoControl - lastErr) * 100);
                        lastErr = sVcoControl;


                        int lockTime = (int)(0.3 * 44100.0);
                        if (sErrTrend < 0.04)
                        {
                            if (lockedSamples < lockTime)
                                lockedSamples++;
                        }
                        else
                            lockedSamples = 0;
                        float isLocked = (lockedSamples >= lockTime) ? 1f : 0f;

                        // Adjust VCO with integrated signal
                        vcoRef.Tune(sVcoControl);

                        if ((i % 100) == 0)
                        {
                            Console.WriteLine("{0,7:F2} {1,7:F2}", vcoRef.GetFrequency(), vcoRef.GetFrequency(fourTimesIndex));
                        }

                        // Decode IQ signal

                        float sI = (float)fDataI.filter(vcoRef.Sin() * sOriginalInput) * 3.5f;
                        float sQ = (float)fDataQ.filter(vcoRef.Cos() * sOriginalInput) * 3.5f;

                        // Write file
                        float sFileInput = sInput;
                        sFileInput = sOriginalInput;

                        //sFileInput = (float)Sin2Square(sInput, 0.063);
                        //sFileInput = (float)fPeak1.filter(sInput);

                        fs.Write(BitConverter.GetBytes(sFileInput), 0, 4);
                        fs.Write(BitConverter.GetBytes(sPhase), 0, 4);
                        fs.Write(BitConverter.GetBytes(sVcoControl), 0, 4);
                        fs.Write(BitConverter.GetBytes(sI), 0, 4);
                        fs.Write(BitConverter.GetBytes(sQ), 0, 4);
                        //fs.Write(BitConverter.GetBytes((float)(-vcoRef.Sin(fourTimesIndex) * sInput)), 0, 4);
                        fs.Write(BitConverter.GetBytes(sRef), 0, 4);
                        fs.Write(BitConverter.GetBytes(sErrTrend), 0, 4);

                    }
                }
            }
        }

        static void FMModulate()
        {
            Vco vco = new Vco(44100, 10000, 2000);
            BiQuadraticFilter bandPass = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 4000, 44100, 0.707);

            using(Stream sInput = File.OpenRead("G:\\TestVoice4_44k_1ch.pcm32f"))
            {
                using (Stream sOutput = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_2_FM.pcm32f")))
                {
                    int bytesRead = 0;
                    byte[] buffer = new byte[4];
                    while ((bytesRead = sInput.Read(buffer, 0, 4)) > 0)
                    {
                        float sample = BitConverter.ToSingle(buffer, 0);

                        vco.Tune(bandPass.filter(sample));
                        vco.Next();

                        sOutput.Write(BitConverter.GetBytes((float)(vco.Cos())), 0, 4);

                    }
                }
            }
        }

        static void Costas2()
        {
            Random r = new Random(7);
            Vco vcoRef = new Vco(44100, 1405, 100);
            int multIndex = vcoRef.AddMultiplier(1);
            //vcoRef.SetPhaseOffset(0, (45 * Math.PI / 180.0));
            //vcoRef.SetPhaseOffset(multIndex, (45 * Math.PI / 180.0));

            // Filters
            BiQuadraticFilter fArmI = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 300, 44100, 0.707);
            BiQuadraticFilter fArmQ = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 300, 44100, 0.707);

            //BiQuadraticFilter fBandpass = new BiQuadraticFilter(BiQuadraticFilter.Type.BANDPASS, 1000, 44100, 10);
            BiQuadraticFilter fBandpass1 = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 1200, 44100, 0.707);
            BiQuadraticFilter fBandpass2 = new BiQuadraticFilter(BiQuadraticFilter.Type.HIGHPASS, 800, 44100, 0.707);

            //BiQuadraticFilter fTwoTimes = new BiQuadraticFilter(BiQuadraticFilter.Type.HIGHPASS, 1000, 44100, 0.707);

            BiQuadraticFilter fDataI = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 450, 44100, 0.707);
            BiQuadraticFilter fDataQ = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 450, 44100, 0.707);

            //BiQuadraticFilter fPeak1 = new BiQuadraticFilter(BiQuadraticFilter.Type.PEAK, 4000, 44100, 10, 36);



            // PI filter
            float pTerm = 0f;
            float iState = 0f;
            float iTerm = 0f;

            float pGain = 0.707f;
            float iGain = (1f / 44100f) * 30f; //0.001f;

            using (Stream fs = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_1.pcm32f")))
            {
                long sampleCount = 44100 * 2;
                byte[] sampleBuffer = new byte[4];
                string inputFile = "";
                //inputFile = "G:\\qpsk31_1khz.44k1.1ch.pcm32f";
                //inputFile = "G:/qpsk_real_microphone_filtered.44k1.1ch.pcm32f";
                //inputFile = "G:/TestVoice3_fm2_44k_1ch.pcm32s";
                inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_3_QPSK.pcm32f");
                //inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_4_QPSK_microphone.pcm32f");
                //inputFile = "G:/rtty45.44k1.1ch.pcm32f";
                //inputFile = "G:/sstv_bs12sec.pcm32f";
                //inputFile = "G:/4xCarrier.44k1.1ch.pcm32f";

                using (Stream fsIn = File.OpenRead(inputFile))
                {
                    sampleCount = fsIn.Length / 4;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        fsIn.Read(sampleBuffer, 0, 4);
                        //if (i < 28000)
                        //    continue;

                        // Advance oscillator
                        vcoRef.Next();


                        float sRefI = (float)vcoRef.Cos(0);
                        float sRefQ = (float)-vcoRef.Sin(0);
                        float sInput = BitConverter.ToSingle(sampleBuffer, 0);

                        //sInput = sInput * sInput;
                        //sInput = (float)fTwoTimes.filter(sInput);

                        // Add noise
                        //sInput += (float)(r.NextDouble() * 2.0 - 1.0) * 0.126f;
                        //sInput *= 3f;
                        sInput = (float)Clip(sInput, 1.0);

                        //sInput = (float)fBandpass.filter(sInput) * 1.3f;
                        //sInput = (float)fBandpass2.filter(fBandpass1.filter(sInput)) * 2f;
                        float sOriginalInput = sInput;


                        // Phase detection
                        float sPhaseI = sInput * sRefI;
                        float sPhaseQ = sInput * sRefQ;

                        float sPhaseI1 = sPhaseI;
                        float sPhaseQ1 = sPhaseQ;

                        sPhaseI = (float)fArmI.filter(sPhaseI);
                        sPhaseQ = (float)fArmQ.filter(sPhaseQ);


                        float sPhaseMixI = (float)Sign(sPhaseI) * sPhaseQ;
                        float sPhaseMixQ = (float)Sign(sPhaseQ) * sPhaseI;


                        float sPhaseFinal = sPhaseMixI - sPhaseMixQ;
                        //sPhaseFinal = sPhaseI * sPhaseQ;

                        // Run PI filter (integrator)
                        pTerm = sPhaseFinal * pGain;

                        iState += sPhaseFinal;
                        iTerm = iGain * iState;
                        if (iState * iGain > 1f)
                            iState = 1f / iGain;
                        else if (iState * iGain < -1f)
                            iState = -1f / iGain;

                        float sVcoControl = pTerm + iTerm;
                        //sVcoControl = (float)fVco.filter(sVcoControl);

                        //// Multiplying by 100 ~= 40dB increase
                        //float sErrTrend = (float)fErr1.filter(Math.Abs(sVcoControl - lastErr) * 100);
                        //lastErr = sVcoControl;


                        //int lockTime = (int)(0.3 * 44100.0);
                        //if (sErrTrend < 0.04)
                        //{
                        //    if (lockedSamples < lockTime)
                        //        lockedSamples++;
                        //}
                        //else
                        //    lockedSamples = 0;
                        //float isLocked = (lockedSamples >= lockTime) ? 1f : 0f;

                        // Adjust VCO with integrated signal
                        //if (i < 0.5 * 44100)
                        vcoRef.Tune(sVcoControl);

                        if ((i % 100) == 0)
                        {
                            Console.WriteLine("{0,7:F2}", vcoRef.GetFrequency());//, vcoRef.GetFrequency(fourTimesIndex));
                        }

                        // Decode IQ signal
                        sPhaseI = (float)fDataI.filter(vcoRef.Cos(multIndex) * sOriginalInput);
                        sPhaseQ = (float)fDataQ.filter(-vcoRef.Sin(multIndex) * sOriginalInput);

                        // Write file
                        float sFileInput = sInput;
                        sFileInput = sOriginalInput;

                        //sFileInput = (float)Sin2Square(sInput, 0.063);
                        //sFileInput = (float)fPeak1.filter(sInput);

                        fs.Write(BitConverter.GetBytes(sFileInput), 0, 4);
                        fs.Write(BitConverter.GetBytes(sPhaseFinal), 0, 4);
                        fs.Write(BitConverter.GetBytes(sVcoControl), 0, 4);
                        fs.Write(BitConverter.GetBytes(sPhaseI), 0, 4);
                        fs.Write(BitConverter.GetBytes(sPhaseQ), 0, 4);

                        //fs.Write(BitConverter.GetBytes((float)Sin2Square(sPhaseI)), 0, 4);
                        //fs.Write(BitConverter.GetBytes((float)Sin2Square(sPhaseQ)), 0, 4);
                        //fs.Write(BitConverter.GetBytes(sPhaseI), 0, 4);
                        //fs.Write(BitConverter.GetBytes(sPhaseQ), 0, 4);

                    }
                }
            }
        }

        static void GenerateQPSK()
        {
            Vco vco = new Vco(44100, 1350);
            Random r = new Random(11);
            Random rNoise = new Random(27);

            long sampleCount = 44100 * 3;
            long preambleSamples = (long)(0.5 * 44100);
            long postambleSamples = (long)(0.2 * 44100);
            double baud = 1400;// 31.25;
            long bitLengthSamples = (long)(44100.0 / baud); //(long)(0.0033 * 44100);

            float bitI = 0;
            float bitQ = 0;

            float lastBit = 0;

            RootRaisedCosineFilter rBitI = new RootRaisedCosineFilter(44100, 5, (float)baud, 0.4f);
            RootRaisedCosineFilter rBitQ = new RootRaisedCosineFilter(44100, 5, (float)baud, 0.4f);

            //BiQuadraticFilter fBandpass = new BiQuadraticFilter(BiQuadraticFilter.Type.BANDPASS, 1400, 44100, 5);

            int ampDiv = 1;

            float[] symbolsPSK = new float[] { -1f, 1f };
            float[] symbols16QAM = new float[] { -1f, -0.33f, 0.33f, 1f };
            float[] symbols32QAM = new float[] { -1f, -0.6f, -0.2f, 0.2f, 0.6f, 1f };
            float[] symbols64QAM = new float[] { -1f, -0.71f, -0.43f, -0.14f, 0.14f, 0.43f, 0.71f, 1f };

            float[] symbols = symbols16QAM;

            using (Stream sOutput = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_3_QPSK.pcm32f")))
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    if (i % bitLengthSamples == 0)
                    {
                        if (i <= preambleSamples)
                        {
                            bitI = lastBit == symbols[0] ? symbols[symbols.Length-1] : symbols[0];
                            bitQ = bitI;

                            lastBit = bitI;

                            //bitI = 1;
                            //bitQ = 0;
                        }
                        else if (i >= sampleCount - postambleSamples)
                        {
                            bitI = ampDiv;
                            bitQ = ampDiv;
                        }
                        else
                        {
                            // QPSK (4-QAM) 2-level signals
                            //bitI = symbols[r.Next(2)];
                            //bitQ = symbols[r.Next(2)];

                            // 16-QAM 4-level signals
                            bitI = symbols[r.Next(4)];
                            bitQ = symbols[r.Next(4)];

                            // 32-QAM (36-QAM technically here) 6-level signals
                            //bitI = symbols[r.Next(6)];
                            //bitQ = symbols[r.Next(6)];

                            // 64-QAM 8-level signals
                            //bitI = symbols[r.Next(8)];
                            //bitQ = symbols[r.Next(8)];
                        }
                    }
                    else
                    {
                        bitI = 0;
                        bitQ = 0;
                    }

                    vco.Next();
                    float sBitI = (float)rBitI.Process(bitI / ampDiv);
                    float sBitQ = (float)rBitQ.Process(bitQ / ampDiv);
                    float sampleI = 0.5f * (float)vco.Cos(0) * sBitI;
                    float sampleQ = 0.5f * (float)-vco.Sin(0) * sBitQ;

                    

                    float sampleOutput = (float)/*fBandpass.filter*/(sampleI + sampleQ);

                    sOutput.Write(BitConverter.GetBytes(sampleOutput), 0, 4);
                    sOutput.Write(BitConverter.GetBytes(bitI / ampDiv), 0, 4);
                    sOutput.Write(BitConverter.GetBytes(bitQ / ampDiv), 0, 4);
                    //sOutput.Write(BitConverter.GetBytes((float)fBitI.filter(bitI)), 0, 4);
                    //sOutput.Write(BitConverter.GetBytes((float)fBitQ.filter(bitQ)), 0, 4);

                    if (i % 1000 == 0)
                    {
                        Console.WriteLine("{0:P0}", (float)i / sampleCount);
                    }
                }
            }
        }

        static void RootRaisedCosineTest()
        {
            Random r = new Random(11);
            Random rNoise = new Random(25);
            int sampleRate = 2000;
            int lastPulse = 0;
            double baud = 250;
            long bitLengthSamples = (long)(sampleRate / baud); //(long)(0.0033 * 44100);
            long totalSymbols = 1000;
            long sampleCount = totalSymbols * bitLengthSamples;
            sampleCount += bitLengthSamples * 15;

            long preambleSymbols = (long)(0.5 * baud);

            GenerateImpulseRRC(sampleRate, 1.0 / baud, 5, 0.4);


            //double[] testSamples = new double[sampleCount];
            //for (int i = 0; i < testSamples.Length; i++)
            //{
            //    testSamples[i] = 1.0;
            //}
            //double test = FIRTest(testSamples, 0, 44100.0, (1.0 / baud), 5);

            double[] symbolsUpsample = new double[sampleCount + (bitLengthSamples * 2)];
            long sampleStuff = (long)(sampleCount / totalSymbols);

            int lastSymbol = -1;
            for (long i = sampleStuff + (bitLengthSamples * 5); i < sampleCount; i += sampleStuff)
            {
                if (preambleSymbols-- > 0)
                {
                    symbolsUpsample[i] = (double)lastSymbol;
                    if (lastSymbol == 1)
                        lastSymbol = -1;
                    else
                        lastSymbol = 1;
                }
                else
                {
                    symbolsUpsample[i] = (double)((r.Next(2) - 0.5) * 2);
                }
            }

            using (Stream sOutput = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_5_RRC.pcm32f")))
            {
                for (int i = 0; i < symbolsUpsample.Length; i++)
                {
                    if (i % 100 == 0)
                    {
                        Console.WriteLine("{0:N0} {1:P2}", i, (double)i / symbolsUpsample.Length);
                    }
                    if (i == 1938)
                    {
                        //Debugger.Break();
                    }
                    float sampleOutput = (float)FIRTest(symbolsUpsample, i, sampleRate, (1.0 / baud), 5);
                    //sampleOutput += (float)(rNoise.NextDouble() * 2f - 1f) * 0.25f;
                    sOutput.Write(BitConverter.GetBytes(sampleOutput), 0, 4);

                    //withNoise[i] = (symbols2[i] * 0.126f) + (float)((r.NextDouble() * 2.0 - 1.0) * 0.126);
                    //withNoise[i] = (symbols2[i] * 0.126f);
                }

                //for (int i = 0; i < symbols2.Length; i++)
                //{
                //    if (i % 100 == 0)
                //    {
                //        Console.WriteLine("{0:N0} {1:P2}", i, (double)i / symbols2.Length);
                //    }
                //    if (i == 1938)
                //    {
                //        //Debugger.Break();
                //    }
                //    float sampleOutput = (float)symbols2[i];
                //    float sampleOutput2 = (float)symbolsUpsample[i];

                //    float sampleOutput3 = (float)withNoise[i];
                //    float sampleOutput4 = (float)FIRTest(withNoise, i, 44100.0, (1.0 / baud), 5);

                //    sOutput.Write(BitConverter.GetBytes(sampleOutput), 0, 4);
                //    sOutput.Write(BitConverter.GetBytes(sampleOutput2), 0, 4);
                //    sOutput.Write(BitConverter.GetBytes(sampleOutput3), 0, 4);
                //    sOutput.Write(BitConverter.GetBytes(sampleOutput4), 0, 4);
                //}
            }
        }

        static void TimingRecoveryTest()
        {
            Random r = new Random(11);
            int sampleRate = 2000;
            double baud = 250;
            long bitLengthSamples = (long)(sampleRate / baud); //(long)(0.0033 * 44100);
            long sampleCount = 88200 + bitLengthSamples * 4;

            float[] lastSymbols = new float[8];
            int[] lastSymbolTimes = new int[8];

            byte[] sampleBuffer = new byte[4];

            float prevSymbol = 0f;
            float middleSymbol = 0f;
            float currentSymbol = 0f;

            //int sampleSkip = 658;// 607;
            //int preloadSamples = 3;
            //int sampleWidth = (int)bitLengthSamples+1;// / 2;
            float tweak = 0;
            float error = 0;

            long samplesPerSymbol = (long)(sampleRate / baud);
            long sampleCounter = 0;// 1350;
            int mode = 0;
            float errInt = 0f;
            float errIntGain = (1f / (float)(baud)) * 1f;// 1200f;
            float perGain = 1f;// 7f;// 150f;

            float nowError = 0f;

            GenerateImpulseRRC(sampleRate, 1.0 / baud, 5, 0.4);
            GenerateSincImpulse(8, 9);

            float[] sampleWorkingBuffer = new float[bitLengthSamples * 10];
            double[] sampleWorkingBuffer2 = new double[sampleWorkingBuffer.Length];
            double[] sampleWorkingBuffer3 = new double[(_sincImpulse.Length - 1) / _sincSliceCount + 1];
            byte[] byteWorkingBuffer = new byte[sampleWorkingBuffer.Length * 4];

            float[] sincDelayQueue = new float[5];
            int delayIndex = 0;

            bool symbolFlipFlop = true;
            int ctr = 0;

            Downsampler res = new Downsampler(sampleRate);
            res.SetRatio(500f / 2000f);

            using (Stream fsOutput = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_6_Timing.pcm32f")))
            {
                using (Stream fsIn = File.OpenRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_7_SNR.pcm32f")))
                {
                    sampleCount = fsIn.Length / 4;// / 2;

                    int bytesRead = 0;
                    while ((bytesRead = fsIn.Read(byteWorkingBuffer,0,byteWorkingBuffer.Length)) > 0)
                    {
                        // Convert into sample buffer
                        for (int i = 0; i < bytesRead; i += 4)
                        {
                            sampleWorkingBuffer[i / 4] = BitConverter.ToSingle(byteWorkingBuffer, i);
                            //sampleWorkingBuffer[i / 4] += (float)(r.NextDouble() * 2.0 - 1.0) * 1.75f;
                        }

                        // Now we can operate on sample values
                        for (int i = 0; i < sampleWorkingBuffer.Length; i++, sampleCounter--)
                        {
                            // Run through matched filter
                            ShiftArrayLeft(sampleWorkingBuffer2, sampleWorkingBuffer[i]);
                            float sample0 = (float)FIRTest(sampleWorkingBuffer2, 0, sampleRate, 1.0 / baud, 5);
                            //sample /= 31f; // Attenuate because loud
                            //sample /= 6f;
                            //sample /= 3f;

                            //Console.WriteLine("In");
                            if (res.Next())
                            {
                                float sample = res.GetSample();
                                //Console.WriteLine("Out");

                                prevSymbol = middleSymbol;
                                middleSymbol = currentSymbol;
                                currentSymbol = sample;

                                if (symbolFlipFlop && Math.Sign(currentSymbol) != Math.Sign(prevSymbol))
                                {
                                    error = middleSymbol * (Math.Sign(currentSymbol) - Math.Sign(prevSymbol));
                                    //error = -error;

                                    errInt += error;
                                    float iState = errInt * errIntGain;
                                    float pState = error * perGain;

                                    nowError = iState + pState;


                                    // Target should revolve around 2x baud rate
                                    float ratio = (float)((baud * 2.0) / sampleRate) + (0.005f * nowError);
                                    Console.WriteLine("[{1}] {0:F4} {2:F6} {3:F6}", ratio, ctr, error, nowError);


                                    res.SetRatio(ratio);
                                }

                                if (symbolFlipFlop)
                                {
                                    fsOutput.Write(BitConverter.GetBytes(sample), 0, 4);
                                    fsOutput.Write(BitConverter.GetBytes(nowError), 0, 4);
                                }
                                ctr++;

                                symbolFlipFlop = !symbolFlipFlop;



                                //Console.WriteLine("Out");

                                //ShiftArrayLeft(sampleWorkingBuffer3, sample);
                                //ShiftArrayLeft(sincDelayQueue, sample);

                                //float sample2 = (float)(SincFIR(sampleWorkingBuffer3, 0, delayIndex));

                                //float symbol = 0f;
                                //if (sampleCounter == samplesPerSymbol / 2)
                                //{
                                //    // Shift samples
                                //    //prevSymbol = middleSymbol;
                                //    //middleSymbol = currentSymbol;
                                //    //currentSymbol = sample;

                                //    middleSymbol = sample;

                                //    //float fakeSample = (tweak*);

                                //    //sampleCounter = samplesPerSymbol;
                                //   // mode = (mode + 1) % 2;


                                //}

                                //if (sampleCounter == 0)
                                //{
                                //    prevSymbol = currentSymbol;
                                //    currentSymbol = sample;

                                //    //error = (currentSymbol - prevSymbol) * middleSymbol;
                                //    //error = -error;// * (bitLengthSamples / 2);

                                //    //error *= 1f;

                                //    error = middleSymbol * (Math.Sign(prevSymbol) - Math.Sign(currentSymbol));

                                //    //samplesPerSymbol += (long)(200 * -error);
                                //    //if (samplesPerSymbol < (sampleRate / baud / 2))
                                //    //    samplesPerSymbol = (long)(sampleRate / baud / 2);
                                //    //else if (samplesPerSymbol > (sampleRate / baud * 2))
                                //    //    samplesPerSymbol = (long)(sampleRate / baud * 2);

                                //    symbol = currentSymbol;
                                //    //symbol = 1;

                                //    //sampleCounter = samplesPerSymbol + (long)(200 * -error);

                                //    errInt += error;
                                //    float iState = errInt * errIntGain;
                                //    float pState = error * perGain;

                                //    nowError = iState + pState;



                                //    sampleCounter = samplesPerSymbol;// + (long)nowError;


                                //    delayIndex = (int)(nowError / (1f / 8f));
                                //    if (delayIndex < 0)
                                //        delayIndex = 0;
                                //    else if (delayIndex > 7)
                                //        delayIndex = 7;

                                //}



                                //symbol = (i % bitLengthSamples) == 0 ? 1f : 0;

                                //fsOutput.Write(BitConverter.GetBytes(sample), 0, 4);
                                //fsOutput.Write(BitConverter.GetBytes((float)sampleWorkingBuffer2[sampleWorkingBuffer2.Length-1]), 0, 4);
                                //fsOutput.Write(BitConverter.GetBytes(nowError/*errInt * errIntGain*/), 0, 4);
                                //fsOutput.Write(BitConverter.GetBytes(symbol), 0, 4);

                                //sample = sincDelayQueue[0];
                                //fsOutput.Write(BitConverter.GetBytes(sample), 0, 4);
                                //fsOutput.Write(BitConverter.GetBytes(sample2), 0, 4);


                            }
                            res.SupplyInput(sample0);
                        }
                    }

                    //for (int i = 0; i < sampleSkip; i++)
                    //{
                    //    fsIn.Read(sampleBuffer, 0, 4);
                    //    fsIn.Read(sampleBuffer, 0, 4);
                    //}

                    //for (int i = 0; i < sampleCount; i++)
                    //{
                    //    int bytesRead = fsIn.Read(sampleBuffer, 0, 4);
                    //    if (bytesRead == 0)
                    //        break;

                    //    float sample = BitConverter.ToSingle(sampleBuffer, 0) / 2.4f;// * 0.18f;
                    //    //fsIn.Read(sampleBuffer, 0, 4); // We don't care about the second channel

                    //    //float filterSample = sample * (float)(RRC((i - 930) / 44100.0, 1.0 / baud, 0.3));
                    //    float filterSample = (float)FIRTest()

                    //    lastSample = currentSample;
                    //    currentSample = futureSample;
                    //    futureSample = sample;

                    //    if (preloadSamples > 0)
                    //    {
                    //        preloadSamples--;
                    //        continue;
                    //    }


                    //    float err = (futureSample - lastSample) * currentSample;
                    //    //sampleWidth += (int)(err * 0.1f);

                    //    //fsOutput.Write(BitConverter.GetBytes(lastSample), 0, 4);
                    //    //fsOutput.Write(BitConverter.GetBytes(currentSample), 0, 4);
                    //    //fsOutput.Write(BitConverter.GetBytes(futureSample), 0, 4);
                    //    //fsOutput.Write(BitConverter.GetBytes(err), 0, 4);

                    //    fsOutput.Write(BitConverter.GetBytes(sample), 0, 4);
                    //    fsOutput.Write(BitConverter.GetBytes(filterSample), 0, 4);

                    //    //// Cheap decimation
                    //    //for (int p = 0; p < sampleWidth; p++)
                    //    //{
                    //    //    fsIn.Read(sampleBuffer, 0, 4);
                    //    //    fsIn.Read(sampleBuffer, 0, 4);
                    //    //}
                    //}
                }
            }
        }

        static void Const()
        {
            //byte[] data = File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SignalTest_6_Timing_isolated.pcm32f"));
            //byte[] data = File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SignalTest", "SignalTest_6_Timing.pcm32f"));
            byte[] data = File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SignalTest", "SignalTest_1.pcm32f"));
            float[] samples = new float[data.Length / 4];
            Buffer.BlockCopy(data, 0, samples, 0, data.Length);

            DrawingVisual dv = new DrawingVisual();
            var dc = dv.RenderOpen();

            int width = 480;
            int height = 480;

            dc.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, width, height));

            Brush pointBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

            // BPSK
            dc.DrawLine(new Pen(Brushes.Black, 1), new System.Windows.Point(width / 2, 0), new System.Windows.Point(width / 2, height));

            //// QPSK
            dc.DrawLine(new Pen(Brushes.Black, 1), new System.Windows.Point(0, height / 2), new System.Windows.Point(width, height / 2));
            //dc.DrawEllipse(pointBrush, null, new System.Windows.Point(width / 4, height / 4), 8, 8);
            //dc.DrawEllipse(pointBrush, null, new System.Windows.Point(width / 4 * 3, height / 4), 8, 8);
            //dc.DrawEllipse(pointBrush, null, new System.Windows.Point(width / 4 * 3, height / 4 * 3), 8, 8);
            //dc.DrawEllipse(pointBrush, null, new System.Windows.Point(width / 4, height / 4 * 3), 8, 8);

            //// 16-QAM - Horizontal
            //dc.DrawLine(new Pen(Brushes.Black, 1), new System.Windows.Point(0, height / 4), new System.Windows.Point(width, height / 4));
            //dc.DrawLine(new Pen(Brushes.Black, 1), new System.Windows.Point(0, height * 0.75), new System.Windows.Point(width, height * 0.75));

            //// 16-QAM - Vertical
            //dc.DrawLine(new Pen(Brushes.Black, 1), new System.Windows.Point(width / 4, 0), new System.Windows.Point(width / 4, height));
            //dc.DrawLine(new Pen(Brushes.Black, 1), new System.Windows.Point(width * 0.75, 0), new System.Windows.Point(width * 0.75, height));

            Constellation constellation = Constellation.CreateSquare(4);

            //// 16-QAM/4-QAM: 0.5, width / 4
            //// 64-QAM: 0.66, width / 6
            //double innerWidth = width * 0.5;
            //double innerHeight = height * 0.5;
            //for (int i = 0; i < constellation.Points.Length; i++)
            //{
            //    Constellation.Point pt = constellation.Points[i];
            //    double ptI = ((pt.I + 1) / 2);
            //    double ptQ = ((pt.Q + 1) / 2);
            //    dc.DrawEllipse(pointBrush, null, new System.Windows.Point((width / 4) + (ptI * innerWidth), (height / 4) + (ptQ * innerWidth)), 3, 3);
            //}

            Brush dotBrush = new SolidColorBrush(Color.FromArgb(127, 0, 127, 255));

            Random r = new Random(13);

            double maxValue = 0.0;
            int channelOffset = 0;
            int channels = 4;
            for (int i = channelOffset; i < samples.Length; i += channels)
            {
                //if (Math.Abs(samples[i]) > maxValue)
                    maxValue += Math.Min(samples[i] * samples[i], 1.0f);
            }
            maxValue /= (samples.Length / channels);
            maxValue = Math.Sqrt(maxValue);

            int sampleCount = 0;
            int sampleInt = 345;
            int sampleDiv = 10;

            for (int i = (1330/*716*/ * channels) + channelOffset; i < samples.Length - (channels* 628/*278*/); i += channels) // sampleCount += sampleInt, i = channels * (sampleCount / sampleDiv))
            {
                //if (Math.Abs(samples[i]) <= (maxValue * 0.05))
                //    continue;

                double sample = samples[i] * (1.0 / maxValue);
                double sample2 = samples[i+1] * (1.0 / maxValue);

                double x = (width / 2) * ((sample * 0.5));// + ((r.NextDouble() * 2 - 1) * 0.01));
                //double y = x;
                x += width / 2;
                //double y = (height / 2) * (samples[i + 1] * 0.95);
                double y = (height / 2) * ((sample2 * 0.5));// + ((r.NextDouble() * 2 - 1) * 0.01));
                y += height / 2;
                // double y = height / 2;
                //dc.DrawRectangle(dotBrush, null, new System.Windows.Rect(x, y, 5, 5));
                dc.DrawEllipse(dotBrush, null, new System.Windows.Point(x, y), 2, 2);
            }

            dc.Close();

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            rtb.Render(dv);

            PngBitmapEncoder pe = new PngBitmapEncoder();
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SignalTest", "const.png");
            using (FileStream outp = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                pe.Frames.Add(BitmapFrame.Create(rtb));
                pe.Save(outp);
            }
        }

        static void AWGN()
        {
            Random r = new Random(23);
            long sampleCount = 0;
            byte[] byteWorkingBuffer = new byte[4 * 3];
            double Es = 0.0f;
            double N0 = 0.0f;
            double snrDb = 40f;
            double snrLin = (float)Math.Pow(10, (snrDb / 10.0));

            using (Stream fsIn = File.OpenRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_3_QPSK.pcm32f")))
            {
                sampleCount = fsIn.Length / 4 / 3;

                int bytesRead = 0;
                while ((bytesRead = fsIn.Read(byteWorkingBuffer, 0, byteWorkingBuffer.Length)) > 0)
                {
                    float sample = BitConverter.ToSingle(byteWorkingBuffer, 0);

                    Es += (float)Math.Pow(Math.Abs(sample), 2);
                }

                Es /= sampleCount;
                N0 = Es / snrLin;
            }

            using (Stream fsOut = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_3_QPSK_2.pcm32f")))
            {
                using (Stream fsIn = File.OpenRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_3_QPSK.pcm32f")))
                {
                    sampleCount = fsIn.Length / 4 / 3;

                    int bytesRead = 0;

                    float EsSigma = (float)Math.Sqrt(N0);
                    while ((bytesRead = fsIn.Read(byteWorkingBuffer, 0, byteWorkingBuffer.Length)) > 0)
                    {
                        float sample = BitConverter.ToSingle(byteWorkingBuffer, 0);

                        float n = (float)NoiseNext(r, EsSigma);
                        sample += n;

                        fsOut.Write(BitConverter.GetBytes(sample), 0, 4);
                        //fsOut.Write(BitConverter.GetBytes(n), 0, 4);
                    }
                }
            }
        }

        static void Costas3()
        {
            float baud = 1400f;// 31.25f;
            Costas c = new SignalTest.Costas(44100, 1355);
            c.ArmFilterHz = baud;

            using (Stream fs = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_1.pcm32f")))
            {
                long sampleCount;
                byte[] sampleBuffer = new byte[4 * 3];
                string inputFile = "";
                //inputFile = "G:\\qpsk31_1khz.44k1.1ch.pcm32f";
                //inputFile = "G:/qpsk_real_microphone_filtered.44k1.1ch.pcm32f";
                //inputFile = "G:/TestVoice3_fm2_44k_1ch.pcm32s";
                inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_3_QPSK_2.pcm32f");
                //inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_4_QPSK_microphone.pcm32f");
                //inputFile = "G:/rtty45.44k1.1ch.pcm32f";
                //inputFile = "G:/sstv_bs12sec.pcm32f";
                //inputFile = "G:/4xCarrier.44k1.1ch.pcm32f";

                using (Stream fsIn = File.OpenRead(inputFile))
                {
                    sampleCount = fsIn.Length / 4 / 1;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        fsIn.Read(sampleBuffer, 0, 4 * 1);
                        float sample = BitConverter.ToSingle(sampleBuffer, 0);

                        c.Process(sample);
                        if (i % 10 == 0)
                        {
                            Console.WriteLine(c.CarrierFrequency);
                        }

                        //if (i > 15000)
                        //{
                        //    c.PreportionalGain = 0.3f;
                        //}

                        float outBitI = c.Inphase();
                        float outBitQ = c.Quadrature();

                        //outBitI = c.Inphase();
                        //outBitQ = c.Quadrature();

                        fs.Write(BitConverter.GetBytes(outBitI), 0, 4);
                        fs.Write(BitConverter.GetBytes(outBitQ), 0, 4);
                        fs.Write(BitConverter.GetBytes(c.ErrorIntegral()), 0, 4);
                        fs.Write(BitConverter.GetBytes(c.Error()), 0, 4);
                        //fs.Write(BitConverter.GetBytes(c.IsLocked ? 1f : 0f), 0, 4);
                    }
                }
            }
        }

        static void TimingRecovery()
        {
            int sampleRate = 44100;
            float baud = 1400f;// 31.25f;

            RootRaisedCosineFilter rBitI = new RootRaisedCosineFilter(sampleRate, 5, baud, 0.4f);
            RootRaisedCosineFilter rBitQ = new RootRaisedCosineFilter(sampleRate, 5, baud, 0.4f);
            Downsampler ds = new Downsampler(sampleRate);
            Downsampler dsQ = new Downsampler(sampleRate);
            ds.SetRatio((baud * 2) / sampleRate);
            dsQ.SetRatio((baud * 2) / sampleRate);
            Gardner g = new Gardner(baud);

            using (Stream fs = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_6_Timing.pcm32f")))
            {
                long sampleCount;
                byte[] sampleBuffer = new byte[4 * 4];
                bool flipFlop = false;
                string inputFile = "";
                //inputFile = "G:\\qpsk31_1khz.44k1.1ch.pcm32f";
                //inputFile = "G:/qpsk_real_microphone_filtered.44k1.1ch.pcm32f";
                //inputFile = "G:/TestVoice3_fm2_44k_1ch.pcm32s";
                inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_1.pcm32f");
                //inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_4_QPSK_microphone.pcm32f");
                //inputFile = "G:/rtty45.44k1.1ch.pcm32f";
                //inputFile = "G:/sstv_bs12sec.pcm32f";
                //inputFile = "G:/4xCarrier.44k1.1ch.pcm32f";

                

                using (Stream fsIn = File.OpenRead(inputFile))
                {
                    sampleCount = fsIn.Length / 4 / 4;
                    long outputCount = 0;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        fsIn.Read(sampleBuffer, 0, sampleBuffer.Length);
                        float sample = BitConverter.ToSingle(sampleBuffer, 0);
                        float sampleQ = BitConverter.ToSingle(sampleBuffer, 4);

                        sample = rBitI.Process(sample) / rBitI.DCGain;
                        sampleQ = rBitQ.Process(sampleQ) / rBitQ.DCGain;

                        dsQ.Next();
                        if (ds.Next())
                        {
                            float sample2 = ds.GetSample();

                            float error = g.Process(sample2);
                            float ratio = ((baud * 16) / sampleRate) + (0.7f * error);
                            Console.WriteLine("{0:F6} {1:F4}", (ratio / 8), sampleRate * (ratio / 16));

                            //if (outputCount < 670)
                            {
                                ds.SetRatio(ratio/8f);
                                dsQ.SetRatio(ratio/8f);
                            }

                            if (flipFlop ^= true)
                            {
                                outputCount++;
                                fs.Write(BitConverter.GetBytes(ds.GetSample()), 0, 4);
                                fs.Write(BitConverter.GetBytes(dsQ.GetSample()), 0, 4);
                            }
                        }
                        ds.SupplyInput(sample);
                        dsQ.SupplyInput(sampleQ);

                        //fs.Write(BitConverter.GetBytes(sample), 0, 4);
                        //fs.Write(BitConverter.GetBytes(error), 0, 4);
                    }
                }
            }
        }

        static void DecisionDirected()
        {
            int sampleRate = 44100;
            float baud = 1400;
            Vco carrier = new Vco(44100, 1355, 50);
            RootRaisedCosineFilter rBitI = new RootRaisedCosineFilter(sampleRate, 5, baud, 0.4f);
            RootRaisedCosineFilter rBitQ = new RootRaisedCosineFilter(sampleRate, 5, baud, 0.4f);
            Downsampler ds = new Downsampler(sampleRate);
            Downsampler dsQ = new Downsampler(sampleRate);
            ds.SetRatio((baud * 2) / sampleRate);
            dsQ.SetRatio((baud * 2) / sampleRate);
            Gardner g = new Gardner(baud);
            bool flipFlop = false;
            // It appears that a lower (but not too low!) proportional gain improves performance
            Integrator intAngle = new Integrator(0.1f, (1f / baud) * 5f);
            Integrator intMagnitude = new Integrator(0.1f, (1f / baud) * 20f);
            intMagnitude.Preload(1f);
            //BiQuadraticFilter bandpass = new BiQuadraticFilter(BiQuadraticFilter.Type.LOWPASS, 5000, sampleRate, 0.707);
            AGC agc = new AGC(0.707f, 20f);

            float bitOutI = 0f;
            float bitOutQ = 0f;

            float constGain = 1f;

            //   QPSK: 2
            // 16-QAM: 4
            // 64-QAM: 8
            Constellation constellation = Constellation.CreateSquare(4);
            Constellation constellationSync = Constellation.CreateSquare(2);
            bool isSyncMode = true;

            using (Stream fs = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_1.pcm32f")))
            {
                long sampleCount;
                byte[] sampleBuffer = new byte[4 * 3];
                string inputFile = "";
                inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_3_QPSK_2.pcm32f");
                //inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_4_qam16_mic.pcm32f");
                //inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_4_qam16_mic_2.pcm32f");
                //inputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SignalTest", "SignalTest_4_qam16_mic_vary.pcm32f");

                using (Stream fsIn = File.OpenRead(inputFile))
                {
                    sampleCount = fsIn.Length / 4 / 1;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        fsIn.Read(sampleBuffer, 0, 4 * 1);
                        float sample = BitConverter.ToSingle(sampleBuffer, 0);
                        //sample = (float)bandpass.filter(sample);
                        //sample *= 2f;

                        // Initial pre-processing AGC
                        //sample = Math.Max(Math.Min(agc.Process(sample), 1f), -1f);
                        //sample = agc.Process(sample);


                        carrier.Next();

                        // Demodulate I and Q samples
                        float inphase = sample * (float)carrier.Cos();
                        float quadrature = sample * -(float)carrier.Sin();

                        float iFilter = rBitI.Process(inphase) / rBitI.DCGain;
                        float qFilter = rBitQ.Process(quadrature) / rBitQ.DCGain;
                        //iFilter *= 4.8f;
                        //qFilter *= 4.8f;
                        //iFilter *= 4.5f;
                        //qFilter *= 4.5f;
                        //iFilter *= constGain;
                        //qFilter *= constGain;

                        //agc.Process((Math.Abs(iFilter) + Math.Abs(qFilter)) / 2f);
                        //agc.Process(Math.Max(Math.Abs(iFilter), Math.Abs(qFilter)));
                        //iFilter *= Math.Min(agc.LastGain, 5);
                        //qFilter *= Math.Min(agc.LastGain, 5);
                        agc.ProcessDual(ref iFilter, ref qFilter);


                        //agc.ProcessDual(ref iFilter, ref qFilter);

                        // Timing recovery
                        dsQ.Next();
                        if (ds.Next())
                        {
                            float sample2 = ds.GetSample();

                            float error = g.Process(sample2);
                            float ratio = ((baud * 2) / sampleRate) + (0.0125f * error);
                            Console.WriteLine("{0:F6} {1:F4} {2:F4}", ratio, sampleRate * ratio * 0.5f, carrier.GetFrequency(0));

                            //if (outputCount < 670)
                            {
                                ds.SetRatio(ratio);
                                dsQ.SetRatio(ratio);
                            }

                            if (flipFlop ^= true)
                            {
                                bitOutI = ds.GetSample();
                                bitOutQ = dsQ.GetSample();

                                bitOutI *= constGain * 0.707f;
                                bitOutQ *= constGain * 0.707f;

                                fs.Write(BitConverter.GetBytes(bitOutI), 0, 4);
                                fs.Write(BitConverter.GetBytes(bitOutQ), 0, 4);
                                fs.Write(BitConverter.GetBytes(0f), 0, 4);
                                fs.Write(BitConverter.GetBytes(0f), 0, 4);

                                // Find the closest constellation point
                                Constellation.Point constPt;
                                if (isSyncMode)
                                    constPt = constellationSync.FindNearestPoint(bitOutI, bitOutQ);
                                else
                                    constPt = constellation.FindNearestPoint(bitOutI, bitOutQ);
                                
                                Console.WriteLine("C {0,5:F2} {1,5:F2}", constPt.I, constPt.Q);
                                Console.WriteLine("G {0,5:F2}", agc.LastGain);

                                //if (i >= 19000)
                                //    Debugger.Break();

                                double curAngle = Math.Atan2(bitOutQ, bitOutI);
                                double constAngle = Math.Atan2(constPt.Q, constPt.I);

                                double curMag = Math.Sqrt((bitOutI * bitOutI) + (bitOutQ * bitOutQ));
                                double constMag = Math.Sqrt((constPt.I * constPt.I) + (constPt.Q * constPt.Q));

                                constGain = intMagnitude.Process((float)(constMag - curMag) * 2f);
                                //constGain = 1.5f;
                                if (constGain > 3.5f)
                                    constGain = 3.5f;


                                curMag = Math.Sqrt((bitOutI * bitOutI) + (bitOutQ * bitOutQ));


                                Console.WriteLine("M {0,5:F2} {1,5:F2} {2,5:F2}", curMag, constMag, constGain);


                                if (Math.Sign(curAngle) == Math.Sign(constAngle))
                                {
                                    double angleDiff = (curAngle - constAngle) * 2.0f;// / 3.1419526535897932384;
                                    //if (i >= 2000)
                                    //    angleDiff /= 2;

                                    // TODO: Add actual sync/preamble detector
                                    if (isSyncMode && i >= 24000)
                                        isSyncMode = false;

                                    float angleFilter = intAngle.Process((float)angleDiff);
                                    Console.WriteLine("A {0,5:F2} {1,5:F2}", angleDiff, angleFilter);
                                    Console.WriteLine("S {0,5}", isSyncMode);

                                    carrier.Tune(angleFilter);

                                    //if (i % 10 == 0)
                                    //{
                                    //    //Console.WriteLine(carrier.GetFrequency(0));
                                    //}
                                }
                                else
                                {

                                }
                            }
                        }
                        ds.SupplyInput(iFilter);
                        dsQ.SupplyInput(qFilter);

                        //fs.Write(BitConverter.GetBytes(sample), 0, 4);
                        //fs.Write(BitConverter.GetBytes((float)carrier.Cos()), 0, 4);
                        //fs.Write(BitConverter.GetBytes(iFilter), 0, 4);
                        //fs.Write(BitConverter.GetBytes(qFilter), 0, 4);
                        bitOutI = bitOutQ = 0f;
                        //fs.Write(BitConverter.GetBytes(c.ErrorIntegral()), 0, 4);
                        //fs.Write(BitConverter.GetBytes(c.Error()), 0, 4);
                        ////fs.Write(BitConverter.GetBytes(c.IsLocked ? 1f : 0f), 0, 4);
                    }
                }
            }
        }


        static double[] _rrcImpulse;
        static void GenerateImpulseRRC(double sampleRate, double symbolLengthSec, int symbolSpan, double rolloff)
        {
            int length = (int)(symbolLengthSec * symbolSpan * sampleRate);
            _rrcImpulse = new double[length];
            double sampleTime = 1.0 / sampleRate;

            for (int i = 0; i < length; i++)
            {
                double time = (i * sampleTime) - (length * sampleTime) / 2;

                _rrcImpulse[i] = RRC(time, symbolLengthSec, rolloff);
            }
        }

        static double[] _sincImpulse;
        static int _sincSliceCount;
        static void GenerateSincImpulse(int slices, int lengthSamples)
        {
            int start = -((lengthSamples - 1) / 2) * slices;
            int end = ((lengthSamples - 1) / 2) * slices;

            _sincSliceCount = slices;
            _sincImpulse = new double[(lengthSamples - 1) * slices + 1];

            for (int i = start; i <= end; i++)
            {
                _sincImpulse[i - start] = sinc(i, slices);
            }
        }

        static double SincFIR(double[] samples, int offset, int delaySliceIndex)
        {
            if (samples[offset + 8] != 0)
            {
                //Debugger.Break();
            }
            double result = 0.0;
            for (int i = delaySliceIndex; i < _sincImpulse.Length; i += _sincSliceCount)
            {
                result += _sincImpulse[i] * samples[(offset + i) / _sincSliceCount];
            }

            return result;
        }

        static double FIRTest(double[] samples, int offset/*, int length*/, double sampleRate, double symbolLengthSec, int symbolSpan)
        {
            double sampleTime = 1.0 / sampleRate;

            // Total block length(sec) = symbol time(sec) * span
            // 300 baud = 0.00333 seconds/symbol
            // Filter length = 0.00333 * 9
            //               = 0.03 sec
            // In samples @44100 Hz = 1323.0 samples

            int length = (int)(symbolLengthSec * symbolSpan * sampleRate);

            double filterTotalTime = symbolLengthSec * symbolSpan;

            double result = 0.0;
            double lastResult = 0.0;
            int symbolCount = 0;
            for (int i = offset; i < Math.Min(offset + length, samples.Length); i++)
            {
                double time = ((i - offset) * sampleTime) - (length * sampleTime) / 2;
                double rrc = _rrcImpulse[i - offset];//RRC(time, symbolLengthSec, 0.4);
                rrc /= 2.4f;
                rrc /= 2f;
                //Console.WriteLine("{0,8:F5} {1,2:F0} {2,8:F5} {3,8:F5} {4,8:F5} {5}", time, samples[i], rrc, rrc * samples[i], result, symbolCount);

                double window = WelchWindow(i - offset, length);

                result += rrc * window * samples[i];

                if (samples[i] != 0)
                {
                    symbolCount++;
                    //Debugger.Break();
                }
                if (result != lastResult)
                {
                    //Debugger.Break();
                }
                lastResult = result;

            }

            return result;
        }

        static double Clip(double value, double threshold)
        {
            if (value > threshold)
                return threshold;
            if (value < -threshold)
                return -threshold;
            return value;
        }

        static double Sin2Square(double value, double threshold = 0.063)
        {
            return value > threshold ? 1.0 : value < -threshold ? -1.0 : 0;
        }

        static double Sign(double value)
        {
            return value >= 0.0 ? 1.0 : -1.0;
        }


        static double RRC1(double time, double symbolLengthSeconds, double alpha)
        {
            double response = 0.0;
            double twoDivPi = 2.0 / Math.PI;
            double fourAlpha = alpha * 4;

            if (time == 0)
            {
                response = (1.0 / Math.Sqrt(symbolLengthSeconds)) * (1.0 - alpha + 4 * (alpha / Math.PI));
                //response /= 40f;
            }
            else if (Math.Abs(time) == (symbolLengthSeconds / fourAlpha))
            {
                response = (alpha / Math.Sqrt(2 * symbolLengthSeconds)) * ((1 + twoDivPi) * Math.Sin(Math.PI / fourAlpha) + (1 - twoDivPi) * Math.Cos(Math.PI / fourAlpha));
            }
            else
            {
                response = (1.0 / Math.Sqrt(symbolLengthSeconds)) * ((Math.Sin(Math.PI * (time / symbolLengthSeconds) * (1.0 - alpha)) + fourAlpha * (time / symbolLengthSeconds) * Math.Cos(Math.PI * (time / symbolLengthSeconds) * (1.0 + alpha))) / (Math.PI * (time / symbolLengthSeconds) * (1.0 - Math.Pow(fourAlpha * (time / symbolLengthSeconds), 2.0))));
                //response /= 40f;
            }

            if (double.IsInfinity(response) || double.IsNaN(response))
            {
                response = 0;
            }

            return response;
        }

        static double RRC(double time, double symbolLengthSeconds, double alpha)
        {
            if (time == 0)
                time = double.Epsilon;
            //else;// if (time == symbolLengthSeconds / (4 * alpha))
                time += 0.000000001;

            double part1 = (Math.Sin(Math.PI * (time/symbolLengthSeconds) * (1 - alpha)) + 4*alpha * (time/symbolLengthSeconds) * Math.Cos(Math.PI * (time / symbolLengthSeconds) * (1 + alpha)));
            double part2 = Math.PI * (time / symbolLengthSeconds) * (1 - Math.Pow(4 * alpha * (time / symbolLengthSeconds), 2));

            double response = part1 / part2;

            return response;
        }

        static void ShiftArrayLeft<T>(T[] array, T newValue)
        {
            for (int i = 1; i < array.Length; i++)
            {
                array[i - 1] = array[i];
            }
            array[array.Length - 1] = newValue;
        }

        static double WelchWindow(int index, int length)
        {
            return 1.0 - Math.Pow(((index - (length - 1) / 2.0)) / ((length - 1) / 2.0), 2);
        }

        static double NoiseNext(Random r, double sigma)
        {
            //double rand = (r.NextDouble() * 2.0 - 1.0);
            double rand = BoxMuller2(r);

            return sigma * rand;
        }

        static double BoxMuller(Random r)
        {
            double u1 = 0.0;
            do
            {
                u1 = r.NextDouble();
            } while (u1 == 0);
            double u2 = r.NextDouble();

            double x = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
            double y = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2);

            return x;
        }

        static double BoxMuller2(Random r)
        {
            double s = 0.0;
            double v1, v2;
            do
            {
                double u1 = r.NextDouble();
                double u2 = r.NextDouble();

                v1 = 2 * u1 - 1;
                v2 = 2 * u2 - 1;

                s = v1 * v1 + v2 * v2;
            } while (s >= 1);

            double x = Math.Sqrt(-2.0 * Math.Log(s) / s) * v1;
            //double y = Math.Sqrt(-2.0 * Math.Log(s) / s) * v2;

            return x;
        }

        static double sinc(double x, double scale)
        {
            if (scale == 0)
                scale = 1;
            if (x == 0)
                return 1;

            double phi = Math.PI / scale;
            return Math.Sin(phi * x) / (phi * x);
        }
    }

    public class Osc
    {
        public double baseFq; // Starting frequency
        public double fq; // Frequency
        public double fqDivisor; // Divisor of base frequency
        public double spanFq; // Adjustable frequency span
        public double w;  // Current phase angle
        public double w2; // Second output phase angle
        public double sampleRate = 44100;
        public double tweak;

        public double FqHz { get { return (fq * sampleRate) / (Math.PI * 2); } }
        public double SubFqHz { get { return ((fq / fqDivisor) * sampleRate) / (Math.PI * 2); } }
        public double BaseFqHz { get { return (baseFq * sampleRate) / (Math.PI * 2); } }
        public double SpanHz { get { return (spanFq * sampleRate) / (Math.PI * 2); } }

        public Osc(double f, double span)
        {
            baseFq = fq = f;
            spanFq = span;
            fqDivisor = 1.0;
            Reset();
        }


        public static Osc FromFrequency(double frequencyHz, double spanHz)//, int samplingRate)
        {
            double samplingRate = 44100;
            return new Osc(2 * Math.PI * frequencyHz * (1.0 / samplingRate), 2 * Math.PI * spanHz / samplingRate);
        }


        public void Reset()
        {
            w = 0;
        }

        public double Sin()
        {
            return Math.Sin(w);// * (1.0 / (2 * Math.PI)));
        }

        public double Cos()
        {
            return Math.Cos(w);// * (1.0 / (2 * Math.PI)));
        }

        public double Sqr()
        {
            return w >= Math.PI ? 1.0 : -1.0;
        }

        public double SinSub()
        {
            return Math.Sin(w2);// * (1.0 / (2 * Math.PI)));
        }

        public double CosSub()
        {
            return Math.Cos(w2);// * (1.0 / (2 * Math.PI)));
        }

        public double SqrSub()
        {
            return w2 >= Math.PI ? 1.0 : -1.0;
        }

        public void Step()
        {
            w += (fq + tweak) % (2 * Math.PI);
            if (w > (2 * Math.PI))
                w -= (2 * Math.PI);

            w2 += ((fq / fqDivisor) + tweak) % (2 * Math.PI);
            if (w2 > (2 * Math.PI))
                w2 -= (2 * Math.PI);
        }

        public double Next()
        {
            double r = Sin();
            Step();
            return r;
        }

        public void Tweak(double tweak)
        {
            this.tweak = tweak;
        }

        public void Adjust(double adjustment)
        {
            fq += adjustment;
            ContainSpan();
        }

        public void AdjustHz(double hzDelta)
        {
            Adjust(2 * Math.PI * hzDelta / sampleRate);
        }

        public void AdjustPercent(double percent)
        {
            Adjust(spanFq * percent);
        }

        public void SetFrequencyHz(double frequencyHz)
        {
            fq = 2 * Math.PI * frequencyHz / sampleRate;
            ContainSpan();
        }

        public void SetFrequency(double frequencyPercent)
        {
            fq = baseFq + (spanFq * frequencyPercent);
            ContainSpan();
        }

        public void SetDivisor(double divisor)
        {
            fqDivisor = divisor;
        }

        public void ContainSpan()
        {
            if (fq > baseFq + spanFq)
                fq = baseFq + spanFq;
            else if (fq < baseFq - spanFq)
                fq = baseFq - spanFq;
        }
    }
}
