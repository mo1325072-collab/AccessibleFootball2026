using System;
using System.IO;
using System.Text;

namespace AccessibleFootball2026.Audio;

/// <summary>
/// مولد ملفات صوت القوائم (UI Navigation & Menu Music Generator):
/// يولد أصوات التنقل النقية وسريعة الاستجابة وموسيقى القوائم الهادئة تلقائياً إذا لم تكن موجودة.
/// </summary>
public static class MenuAudioGenerator
{
    public static void EnsureAllMenuAudio(string menuDir)
    {
        Directory.CreateDirectory(menuDir);
        EnsureMenuMove(Path.Combine(menuDir, "Move.wav"));
        EnsureMenuSelect(Path.Combine(menuDir, "Select.wav"));
        EnsureMenuBack(Path.Combine(menuDir, "Back.wav"));
        EnsureMenuMusic(Path.Combine(menuDir, "MenuMusic.wav"));
    }

    public static void EnsureMenuMove(string path)
    {
        // صوت الحركة المعتمد من المستخدم 2026-11-13: تكتكة خفيفة مكتومة (65ms)
        // يكتب الصوت المعتمد عند غياب الملف أو اختلافه عن النسخة المعتمدة.
        WriteApprovedIfChanged(path, ApprovedMenuMoveWav);
    }

    public static void EnsureMenuSelect(string path)
    {
        // صوت الاختيار المعتمد من المستخدم 2026-11-13: ضغطة تأكيد إيقاعية بطابع قوائم ألعاب الكورة (160ms)
        WriteApprovedIfChanged(path, ApprovedMenuSelectWav);
    }

    public static void EnsureMenuBack(string path)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 100) return;
        int sr = 44100;
        int n = (int)(sr * 0.05); // 50ms soft tap
        short[] s = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            double freq = 600.0 - (250.0 * (t / 0.05));
            double env = Math.Exp(-t * 90.0);
            double wave = Math.Sin(2.0 * Math.PI * freq * t) * env * 11000;
            s[i] = (short)Math.Clamp(wave, -32768, 32767);
        }
        WriteWav(path, s, sr);
    }

    public static void EnsureMenuMusic(string path)
    {
        // لا نستبدل موسيقى قوائم حقيقية وفّرها المستخدم (ملف مرفوع ضمن الحزمة) - نولّد فقط عند الغياب
        if (File.Exists(path) && new FileInfo(path).Length > 100) return;
        string mp3 = Path.ChangeExtension(path, ".mp3");
        if (File.Exists(mp3) && new FileInfo(mp3).Length > 100) return;
        int sampleRate = 44100;
        int seconds = 8;
        int frames = sampleRate * seconds;
        short[] s = new short[frames * 2];

        // Lo-fi progression الأصلية الهادئة: Dm9 -> Gm7 -> C7 -> Fmaj7
        double[] rootFreqs = { 146.83, 98.00, 130.81, 87.31 };

        for (int i = 0; i < frames; i++)
        {
            double t = (double)i / sampleRate;
            int bar = (int)(t / 2.0) % 4;
            double root = rootFreqs[bar];

            double bass = Math.Sin(2.0 * Math.PI * root * t) * 0.30;
            double chord = (Math.Sin(2.0 * Math.PI * root * 1.5 * t) +
                            Math.Sin(2.0 * Math.PI * root * 2.0 * t) +
                            Math.Sin(2.0 * Math.PI * root * 2.5 * t)) * 0.10;

            double beatTime = t % 0.5;
            double kick = (beatTime < 0.05) ? Math.Sin(2.0 * Math.PI * 55.0 * beatTime) * Math.Exp(-beatTime * 45.0) * 0.25 : 0.0;

            double hatTime = t % 0.25;
            double hat = (hatTime < 0.015) ? (new Random((int)(t * 1000)).NextDouble() - 0.5) * Math.Exp(-hatTime * 90.0) * 0.06 : 0.0;

            // مزيج هادئ وخافت جداً بحيث يظل صوت قارئ الشاشة واضحاً بنسبة 100%
            double mix = (bass + chord + kick + hat) * 0.40;
            short sample = (short)Math.Clamp(mix * 12000, -32000, 32000);

            s[i * 2] = sample;
            s[i * 2 + 1] = sample;
        }
        WriteWavStereo(path, s, sampleRate);
    }

    private static void WriteApprovedIfChanged(string path, byte[] approved)
    {
        if (File.Exists(path))
        {
            byte[] existing = File.ReadAllBytes(path);
            if (existing.Length == approved.Length && existing.AsSpan().SequenceEqual(approved)) return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, approved);
    }

    // الأصوات المعتمدة مضمّنة كـ WAV كامل (44100Hz mono 16-bit PCM) حتى تصل مطابقة تماماً لما سمعه المستخدم واعتمد.
    private static byte[] ApprovedMenuMoveWav => Convert.FromBase64String(ApprovedMenuMoveWavBase64);
    private static byte[] ApprovedMenuSelectWav => Convert.FromBase64String(ApprovedMenuSelectWavBase64);

    private const string ApprovedMenuMoveWavBase64 =
        "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" +
        "/gBFAYcBxAH6ASgCUAJvAoYClQKbApoCkAJ/AmcCSQIlAvsBzAGZAWIBKQHtAK8AcAAwAPH/sf9y/zX/+v7B/ov+Wv4s/gP+4P3C" +
        "/ar9mf2Q/Y39kv2f/bP9z/3y/Rv+TP6C/rz+/P4+/4P/yv8QAFcAnADeAB0BWAGOAb4B6AELAicCPAJJAlACTwJHAjgCIwIIAugB" +
        "wwGaAW0BPAEJAdQAnQBlACwA9P+7/4P/Tf8Y/+X+tv6K/mH+Pf4d/gP+7v3f/db91P3Z/eT99v0O/i3+Uv58/qz+4P4Y/1P/kP/P" +
        "/w0ASwCIAMMA+wAvAV8BigGvAc8B6AH6AQcCDAIMAgUC+QHnAc8BswGSAW4BRgEbAe0AvgCNAFsAKAD3/8T/kv9h/zP/Bv/b/rT+" +
        "kP5w/lT+PP4q/h3+Ff4T/hf+If4x/kb+Yv6C/qj+0v4A/zL/Zv+c/9P/CgBBAHcAqwDdAAsBNgFcAX0BmQGwAcABzAHRAdEBywHB" +
        "AbEBnAGDAWYBRgEjAf0A1ACqAH8AUgAlAPn/zP+f/3T/Sv8i//3+2f65/p3+hP5v/l/+U/5M/kv+Tv5X/mX+eP6Q/q3+z/70/h3/" +
        "SP92/6b/1/8HADgAaACWAMIA7AARATMBUQFqAX4BjQGXAZwBnAGYAY4BgQFvAVkBPwEjAQMB4gC+AJgAcgBKACIA+//T/6v/hP9f" +
        "/zz/Gv/7/t7+xf6v/pz+jv6E/n7+fP5//of+lP6k/rr+0/7x/hL/Nv9d/4X/sP/b/wUAMQBbAIQAqwDPAPEADwEpAUABUgFgAWkB" +
        "bgFuAWoBYgFWAUYBMwEcAQMB5wDJAKoAiABmAEMAHwD8/9n/tf+T/3L/Uv80/xn///7p/tX+xf64/q/+qf6o/qv+sv69/sz+3/71" +
        "/g//LP9M/27/kv+4/97/BAAqAE8AdACWALcA1ADvAAcBGwErATcBQAFEAUUBQQE6ATABIgERAf0A5wDOALQAmAB6AFwAPAAcAP7/" +
        "3v++/6D/gv9m/0z/M/8c/wj/9/7o/t3+1f7Q/s/+0f7X/uH+7/7//hP/Kv9E/2D/fv+e/7//4f8CACQARQBmAIQAoQC7ANMA6AD6" +
        "AAgBFAEbAR8BIAEdARcBDgECAfMA4gDOALgAoQCIAG0AUgA2ABoA///i/8f/q/+R/3j/YP9K/zb/JP8V/wj//v72/vL+8f70/vn+" +
        "Av8N/xz/Lv9C/1n/cv+N/6n/xv/k/wEAHwA9AFkAdACOAKUAugDNAN0A6gD0APsA/wD/AP0A+ADwAOUA2ADJALcApACPAHkAYgBK" +
        "ADEAGAAAAOb/zv+1/57/iP9z/1//Tf89/y//JP8b/xX/Ef8Q/xL/F/8e/yn/Nv9F/1f/bP+C/5n/sv/M/+b/AAAbADUATgBmAH0A" +
        "kgClALUAwwDPANgA3gDiAOMA4QDcANUAzADAALMAowCSAIAAbABYAEIALAAWAAAA6v/U/77/qf+V/4P/cf9h/1P/R/89/zX/L/8s" +
        "/yv/Lf8x/zj/Qf9N/1r/av98/5D/pf+7/9H/6f8AABcALgBFAFoAbgCBAJEAoACtALcAvwDFAMgAyQDHAMMAvQC1AKsAnwCSAIIA" +
        "cgBhAE4AOwAoABQAAADt/9n/xv+z/6L/kf+C/3T/Z/9c/1P/TP9H/0T/Q/9F/0j/Tv9X/2H/bf97/4v/nP+v/8L/1v/r/wAAFAAo" +
        "ADwATwBhAHEAgACNAJkAogCpAK4AsQCyALEArgCoAKEAmACOAIIAdABmAFYARgA1ACQAEgAAAO//3v/N/7z/rf+e/5D/hP94/2//" +
        "Z/9g/1z/Wf9Z/1r/Xf9i/2r/c/9+/4r/mP+n/7j/yf/b/+3/AAARACMANQBGAFUAZABxAH0AhwCPAJYAmwCdAJ4AnQCaAJUAjwCH" +
        "AH4AcwBoAFsATQA/ADAAIAAQAAEA8v/i/9P/xP+2/6n/nf+S/4j/f/94/3L/bv9s/2v/bf9w/3T/e/+D/4z/l/+k/7H/v//P/9//" +
        "7////w8AHwAuAD0ASwBYAGQAbgB3AH8AhQCJAIsAjACLAIkAhQB/AHgAcABnAFwAUQBFADgAKwAdAA8AAQD0/+b/2P/L/7//s/+o" +
        "/57/lf+O/4f/gv9//33/fP99/4D/hP+K/5H/mf+j/67/uv/H/9T/4v/w////DAAbACkANgBCAE4AWABiAGoAcAB1AHkAfAB8AHwA" +
        "eQB2AHEAawBkAFsAUgBIAD0AMgAmABoADQABAPb/6f/d/9L/xv+8/7L/qf+i/5v/lf+R/47/jP+L/4z/jv+S/5f/nf+l/63/t//C" +
        "/83/2f/l//L///8LABcAJAAvADoARQBOAFYAXQBjAGgAawBtAG4AbgBsAGkAZABfAFkAUQBJAEAANwAtACIAFwAMAAEA9//s/+H/" +
        "1//N/8T/u/+z/6z/pv+h/53/m/+Z/5j/mf+b/57/o/+o/6//t/+//8j/0v/d/+j/8////wkAFAAfACoAMwA8AEUATABTAFgAXABf" +
        "AGEAYgBhAGAAXQBZAFUATwBIAEEAOQAxACgAHwAVAAsAAQD4/+//5f/c/9P/y//D/7z/tv+x/6z/qf+m/6X/pP+l/6f/qf+t/7L/" +
        "uP+//8b/z//Y/+H/6//1////CAASABsAJQAtADUAPQBDAEkATgBRAFQAVgBXAFYAVQBTAE8ASwBGAEAAOgAzACwAJAAbABMACgAB" +
        "APn/8f/o/+D/2P/R/8r/xP++/7r/tv+y/7D/r/+v/6//sf+z/7f/u//A/8b/zf/U/9z/5P/t//b///8HAA8AGAAgACgALwA2ADsA" +
        "QABFAEgASwBMAE0ATQBLAEkARgBDAD4AOQA0AC0AJwAgABgAEQAJAAEA+v/z/+v/5P/d/9b/0P/L/8b/wv++/7v/uf+4/7j/uP+6" +
        "/7z/v//D/8f/zf/T/9n/4P/n/+//9////wYADQAVABwAIwApAC8ANAA5AD0AQABCAEMARABEAEMAQQA/ADsANwAzAC4AKAAjABwA" +
        "FgAPAAgAAQD7//T/7v/n/+H/2//W/9H/zf/J/8b/w//B/8D/wP/A/8L/xP/G/8r/zv/S/9j/3f/j/+r/8f/4////BQAMABIAGQAf" +
        "ACUAKgAuADIANgA4ADoAPAA8ADwAOwA6ADgANQAxAC0AKQAkAB8AGQATAA0ABwABAPz/9v/w/+r/5f/g/9v/1v/T/8//zP/K/8j/" +
        "yP/H/8j/yf/K/83/0P/T/9j/3P/h/+f/7P/y//j///8EAAoAEAAWABsAIAAlACkALQAwADIANAA1ADUANQA1ADMAMQAvACwAKAAk" +
        "AA==";

    private const string ApprovedMenuSelectWavBase64 =
        "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" +
        "ABj/1f2T/FT7Gfrj+LP3ifZn9U70PfM28jnxRvBf74Luse3s7DPshevj6k3qxOlG6dTobugU6MfnhedQ5yjnDOf95vvmB+cf50bn" +
        "eue85w3oa+jX6FLp2ulw6hTrxOuB7ErtHu7+7ufv2vDV8djy4vPy9Af2IPc9+Fz5ffqg+8L85f0H/yYARQFhAnsDkgSlBbQGwAfH" +
        "CMoJxwrAC7IMnw2FDmQPPBAMEdQRkxJIE/MTlBQqFbQVMhajFgcXXhenF+IXDxguGD4YQBg0GBoY8he8F3kXKhfOFmYW8hV0FesU" +
        "WBS8ExYTaBKyEfQQLxBjD48Otg3WDPELBgsWCiAJJggoByYGIAUXBAsD/QHtAN3/y/65/aj8mfuL+oH5evh393n2gvWQ9Kbzw/Lo" +
        "8RbxTfCO79juLe6M7fXsaezo63LrBuum6lDqBOrE6Y7pY+lC6SvpIOke6SjpO+la6YPptun06T3qkerv6ljry+tJ7NHsZO0A7qbu" +
        "Vu8O8M/wmPFp8kDzHvQC9ev12fbK97/4t/mw+qv7p/yj/Z/+mv+TAIsBggJ2A2cEVQVBBigHDAjsCMcJngpvCzwMAw3FDYAONQ/k" +
        "D4sQKhHCEVES2BJWE8oTNRSWFOwUNxV4Fa0V2BX3FQsWExYRFgMW6hXGFZgVXxUcFdAUehQbFLITQhPJEkgSwBExEZsQ/Q9aD7AO" +
        "AQ5LDZAM0AsLC0EKcwmgCMkH7gYQBi8FSwRlA3wCkwGoAL7/0/7o/f/8F/wx+076b/mT+Lv35/YZ9lH1jvTS8xzzbfLE8STxivD4" +
        "727v7O5x7v7tlO0x7dfshOw67Pjrv+uO62XrResu6x/rGusd6ynrP+td64Xrtuvw6zPsgOzV7DPtmu0K7oHuAe+I7xfwrfBJ8ezx" +
        "lPJC8/XzrPRo9Sf26vav93j4QvkP+t36rft9/E/9IP7y/sX/lQBnATcCBwPVA6IEbQU2BvwGwAeACD4J9wmtCl0LCQywDFIN7Q2C" +
        "DhAPmA8ZEJIQAxFtEc8RKBJ6EsMSAxM8E2wTlBOzE8sT2hPiE+ET2BPIE7ETkRNqEzwTBxPKEocSPBLqEZIRMhHMEGAQ7A9yD/IO" +
        "bA7gDU4NtwwaDHgL0gomCncJxAgOCFQHmAbZBRkFVwSTA88CCwJGAYIAv//9/jv+e/29/AD8RvuP+tn5J/l4+Mz3I/d+9tz1PvWl" +
        "9BD0f/Pz8mvy6fFt8fXwhPAY8LPvVO/87qruX+4c7uDtq+197VjtOe0j7RTtDO0M7RTtI+057Vbte+2m7djtEe5Q7pXu4O4x74jv" +
        "5e9H8K7wG/GM8QPyfvL/8oPzDPSZ9Cv1wPVZ9vb2lvc5+N/4iPkz+uD6j/tA/PL8pf1Z/g3/wf90ACcB2QGJAjkD5gOSBDsF4QWF" +
        "BiYHxAdeCPUIiAkYCqQKKwuvCy4MqQwgDZIN/w1nDsoOKQ+CD9UPJBBsEK8Q7BAjEVMRfhGiEcAR1xHoEfIR9hHzEeoR2hHDEaYR" +
        "gxFaESsR9hC7EHoQNBDpD5gPQw/oDokOJg6+DVEN4QxtDPULegv7CngK8glqCd4ITwi+ByoHlAb7BWEFxQQnBIgD6AJGAqUBAgFg" +
        "AL//Hf97/tv9O/2d/AH8ZvvN+jf6o/kS+YP49/dv9+r2aPbp9W/1+PSE9BX0qfNC897yf/Il8s7xfPEv8ebwo/Bk8Crw9e/F75rv" +
        "de9V7zvvJu8X7w3vCe8K7xHvHu8w70jvZe+I76/v3O8O8EXwgfDB8AbxT/Gd8e/xRfKe8vzyXfPC8yr0lfQE9XX16vVh9tv2WPfX" +
        "91n43fhj+er5dPr/+oz7Gvyp/Dn9yv1c/u3+f/8QAKEAMgHDAVIC4AJtA/kDggQLBZEFFQaXBhYHkwcNCIUI+ghsCdsJRwqvChUL" +
        "dwvWCzEMiQzdDC4Neg3DDQcOSA6FDr0O8Q4gD0sPcQ+TD7APyQ/cD+sP9Q/7D/sP9w/uD+EPzw+4D50Pfg9aDzIPBg/VDqEOaQ4t" +
        "Du0Nqg1jDRgNywx6DCUMzgt0CxYLtgpTCu0JhQkaCawIPQjLB1cH4QZpBvAFdQX4BHsE/AN8A/wCewL6AXgB9wB1APX/dP/0/nT+" +
        "9f13/fr8fvwE/Iv7E/ud+in6tvlG+df4avgA+Jf3MffN9mz2Dvay9Vj1AvWv9F70EfTH84DzPfP98sHyiPJT8iLy9fHL8abxhPFm" +
        "8UzxNvEk8RbxDPEF8QPxBPEJ8RLxHvEu8ULxWvF08ZPxtPHa8QLyLvJd8pDyxvL/8jvzevO88wH0SfSU9OL0M/WG9dz1NPaO9uv2" +
        "Sver9w74cvjY+ED5qfkU+n/67Ppa+8j7N/yn/Bf9iP35/Wr+2/5M/73/LQCeAA4BfgHtAVwCygI3A6MDDwR5BOIESgWxBRYGegbb" +
        "BjwHmgf2B1AIqAj+CFIJownyCT4KhwrOChILUwuSC80LBgw8DG8MnwzMDPYMHQ1BDWINgA2bDbMNyA3aDekN9Q39DQMOBg4FDgEO" +
        "+g3wDeMN0w3ADakNkA1zDVQNMQ0MDeQMuQyLDFoMJwzxC7kLfgtBCwILwAp8CjYK7wmlCVkJDAm9CGwIGgjGB3EHGgfCBmkGDgaz" +
        "BVYF+ASaBDoE2gN5AxgDtgJTAvABjQEqAcYAYwAAAJ3/O//Y/nb+Ff60/VT99fyW/Dn83fuC+yj7z/p3+iH6zPl5+Sf51viI+Dr4" +
        "7/el9133F/fS9pD2T/YR9tT1mvVi9Sz1+PTG9Jf0avRA9Bj08/PQ86/zkfN2813zR/Mz8yLzFPMI8//y+PL08vLy8/L28vzyBPMO" +
        "8xvzKvM881DzZvN/85rzt/PW8/jzHPRB9Gr0lPTA9O/0H/VR9Yb1vPX09S72avan9ub2J/dp96z38fc4+H/4yPgS+V35qfn2+UT6" +
        "kvri+jL7g/vU+yb8ePzL/B/9cv3G/Rr+b/7D/hj/bf/B/xUAaQC+ABIBZQG5AQsCXgKwAgEDUQOhA/ADPgSLBNcEIgVsBbUF/AVD" +
        "BogGzAYOB1AHjwfOBwsIRgiACLgI7wgkCVgJigm6CekJFgpBCmoKkgq4CtwK/goeCz0LWQt0C4wLowu3C8oL2gvpC/ULAAwIDA8M" +
        "EwwWDBYMFQwRDAwMBQz7C/AL4wvVC8QLsgueC4gLcAtXCzwLHwsBC+EKvwqcCncKUAooCv8J1AmnCXkJSgkZCecItAh/CEkIEgja" +
        "B6EHZgcrB+8GsQZzBjQG9QW0BXMFMgXvBKwEaQQlBOEDnANXAxEDywKFAj8C+QGyAWsBJAHdAJYATwAIAMP/fP82/+/+qv5k/h/+" +
        "2v2W/VL9D/3M/Ir8SfwI/Mj7iftK+w370PqU+ln6IPrn+a/5d/lB+Q352fim+HT4Q/gU+Ob3ufeN92L3OfcQ9+n2xPag9n32W/Y7" +
        "9hz2//Xj9cn1sPWY9YL1bvVb9Un1OfUq9R31EfUH9f/09/Tx9O306vTp9On06vTt9PH09vT99Ab1D/Ub9Sf1NfVE9VT1ZvV59Y71" +
        "pPW79dP17fUH9iP2QfZf9n/2n/bB9uT2CPct91P3evei98v39fcg+Ev4d/il+NP4Afkx+WH5kfnD+fX5KPpb+o/6w/r4+i37Y/ua" +
        "+9D7CPw//Hf8r/zo/CH9Wv2T/cz9Bf4//nn+sv7s/ib/X/+Z/9L/CwBEAH0AtgDuACYBXgGWAc4BBQI7AnICqALeAhMDRwN8A7AD" +
        "4wMWBEgEegSrBNsECwU6BWkFlwXEBfAFHAZGBnEGmgbCBuoGEQc2B1sHgAejB8UH5gcHCCcIRQhjCIAImwi2CNAI6QgBCRgJLglD" +
        "CVgJawl9CY4JngmtCc4J6Qn/CRAKGwohCiEKHAoSCgIK7QnTCbQJjwllCTcJAwnLCI4ITQgHCL4HcAceB8kGcAYUBrUFUwXuBIcE" +
        "HgSyA0UD1gJlAvQBggEPAZsAKAC1/0L/z/5d/uz9ff0O/aL8N/zP+2j7Bfuk+kb67PmU+UH58fil+Fz4GfjZ9573Z/c19wj33/a8" +
        "9p32hPZv9mD2VvZR9lH2VvZh9nD2hfae9r324PYI9zX3Z/ed99f3FvhY+J/46vg4+Yr53/k3+pL68fpR+7T7GfyB/On8VP2//Sz+" +
        "mv4I/3b/5f9TAMEALwGcAQcCcgLbAkMDqQMMBG0EzAQoBYIF2AUrBnsGxwYPB1QHlAfRBwkIPQhsCJcIvgjfCPwIFAknCTYJPwlD" +
        "CUMJPgkzCSQJEAn3CNkItwiQCGUINQgBCMgHjAdMBwgHwAZ0BiYG1AV/BSgFzgRxBBIEsQNOA+oChAIdArYBTQHkAHoAEACo/z7/" +
        "1f5t/gb+oP08/dn8ePwZ/Lz7YfsJ+7T6YvoT+sf5fvk5+fj4uviB+Ev4Gvjt98X3oPeB92b3T/c99zD3KPck9yX3K/c190T3WPdw" +
        "9433rvfU9/73LPhe+JT4zvgM+U35kvna+Sb6dPrF+hn7cPvJ+yP8gPzf/D/9of0D/mf+y/4w/5X/+v9eAMMAJwGLAe0BTwKvAg0D" +
        "agPFAx0EcwTHBBgFZwWyBfoFPwaBBr8G+QYwB2IHkQe8B+IHBAgiCDwIUQhiCG4Idgh5CHgIcghoCFkIRgguCBII8gfOB6UHeQdI" +
        "BxQH3AahBmIGIAbaBZIFRgX4BKgEVQQABKkDUAP1ApkCPALdAX4BHgG+AF0A/f+d/z3/3f5+/iD+w/1n/Q39tfxe/An8t/tn+xr7" +
        "z/qH+kL6APrC+Yf5T/kc+ev4v/iX+HL4Uvg2+B74Cvj69+/36Pfm9+f37ff49wb4Gfgw+Ev4aviN+LT43/gO+UD5dvmv+ev5K/pu" +
        "+rP6+/pG+5P74/s0/Ij83fw0/Yz95f1A/pv+9/5T/6//CwBnAMMAHwF6AdQBLQKEAtoCLwOBA9IDIQRtBLcE/gRCBYQFwgX+BTYG" +
        "agacBskG8wYaBzwHWwd1B4wHngetB7cHvQfAB74HuAetB58HjQd3B1wHPgccB/cGzQagBnAGPAYFBssFjgVOBQsFxgR+BDQE6AOa" +
        "A0oD+AKlAlAC+wGkAU0B9QCdAEUA7f+V/z3/5f6P/jn+5P2Q/T797vyf/FL8B/y++3j7NPvz+rT6efpA+gv62fmq+X75V/ky+RL5" +
        "9fjc+Mf4tfio+J74mPiX+Jn4n/iq+Lj4yfjf+Pn4Fvk3+Vv5g/mu+d35DvpD+nv6tvrz+jP7dvu7+wL8S/yW/OL8Mf2A/dH9I/52" +
        "/sn+Hf9y/8b/GgBuAMIAFgFpAbsBDAJcAqoC9wJDA4wD0wMZBFwEnQTbBBYFTwWEBbcF5wUTBjwGYgaFBqMGvwbWBuoG+gYHBxAH" +
        "FQcWBxQHDQcDB/YG5AbPBrYGmgZ7BlcGMQYHBtsFqwV4BUIFCgXPBJEEUQQPBMsDhQM9A/QCqQJdAg8CwQFxASIB0QCAAC8A4P+P" +
        "/z7/7/6f/lH+BP63/W39I/3b/JX8UfwP/M/7kvtW+x776Pq1+oT6V/ot+gb64vnB+aT5ivl0+WH5UvlG+T75Ovk5+Tz5QvlM+Vr5" +
        "a/l/+Zf5svnR+fP5GPpA+mv6mfrK+v76NPtt+6j75fsk/GX8qfzt/DT9fP3F/Q/+Wv6m/vL+P/+M/9r/JgBzAMAADAFYAaMB7QE2" +
        "An0CwwIIA0sDjAPLAwgEQgR7BLEE5AQVBUMFbgWWBbsF3QX8BRcGLwZEBlYGZAZvBncGegZ7BngGcgZoBlsGSgY2Bh8GBQbnBcYF" +
        "owV8BVIFJgX3BMYEkgRbBCME6AOrA2wDLAPqAqcCYgIcAtUBjQFEAfsAsQBoAB4A1f+L/0H/+P6w/mn+Iv7c/Zj9Vf0U/dT8lvxa" +
        "/CD86Puy+3/7Tvsg+/T6y/ql+oL6YvpF+iv6FPoA+u/54vnY+dH5zvnO+dH51/nh+e75/vkR+ij6Qfpe+n36oPrF+u36GPtF+3T7" +
        "pvvb+xH8SfyE/MD8/fw9/X39v/0C/kb+i/7Q/hb/Xf+k/+r/MAB3AL0AAwFIAYwBzwESAlMCkwLRAg4DSQOCA7oD7wMiBFMEggSu" +
        "BNcE/gQiBUQFYgV+BZYFrAW/Bc4F2wXkBeoF7QXtBeoF4wXaBc0FvQWqBZUFfAVgBUIFIQX9BNcErgSCBFQEJATyA74DiANQAxcD" +
        "2wKfAmECIgLiAaABXwEcAdkAlgBSAA4AzP+I/0X/Av/A/n/+P/7//cH9hP1J/Q/91vyg/Gv8OPwI/Nn7rfuD+1v7N/sU+/X62Pq9" +
        "+qb6kvqA+nH6Zvpd+lj6VfpV+ln6X/pp+nX6hPqX+qz6xPre+vz6HPs++2P7ivu0++D7Dvw+/HH8pPza/BH9Sv2E/b/9/P05/nj+" +
        "t/72/jf/d/+4//j/OAB5ALkA+AA3AXYBswHwASsCZQKeAtUCCwM/A3IDogPQA/0DJwRPBHQElwS4BNYE8QQKBSAFMwVEBVIFXAVk" +
        "BWkFbAVrBWcFYQVYBUwFPQUrBRcFAAXmBMoEqwSKBGYEQAQYBO4DwQOTA2MDMQP+AskCkwJbAiIC6AGtAXIBNQH4ALsAfQA/AAEA" +
        "xf+H/0r/Df/R/pX+Wv4h/uj9sP16/UX9Ev3g/LH8gvxW/Cz8BPze+7v7mft6+177RPss+xf7Bfv2+un63/rX+tP60frS+tX63Prl" +
        "+vH6//oQ+yT7OvtT+2/7jPus+8/78/sa/EL8bfyZ/Mf89/wo/Vv9j/3F/fv9M/5r/qT+3v4Y/1P/jv/J/wMAPgB5ALQA7gAoAWAB" +
        "mAHQAQYCOwJuAqEC0gIBAy8DWwOFA60D0wP3AxkEOARWBHEEigSgBLMExQTTBN8E6QTwBPQE9QT0BPAE6gThBNUExwS3BKQEjgR2" +
        "BFwEPwQgBP8D3AO3A5ADZwM9AxED4wKzAoMCUQIeAukBtAF+AUgBEAHZAKAAaAAvAPj/v/+H/0//F//g/qr+dP5A/gz+2v2o/Xj9" +
        "Sv0c/fH8x/yf/Hn8Vfwy/BL89PvY+777p/uS+3/7b/ti+1b7TftH+0P7QvtE+0f7TvtW+2L7b/uA+5L7p/u++9f78/sR/DD8Uvx2" +
        "/Jv8wvzr/Bb9Qv1v/Z79zv3//TH+ZP6X/sz+Af82/2z/ov/Y/w0AQwB5AK4A5AAYAUwBfwGxAeMBEwJCAnACnALHAvECGAM/A2MD" +
        "hgOmA8UD4QP8AxQEKgQ+BFAEXwRsBHcEfwSEBIgEiQSHBIMEfQR0BGkEXARMBDsEJgQQBPgD3QPBA6IDggNfAzsDFgPvAsYCnAJw" +
        "AkMCFQLmAbcBhgFUASIB7wC8AIkAVQAhAO//u/+I/1T/Iv/w/r7+jf5d/i7+AP7T/af9ff1U/S39B/3i/MD8n/yA/GP8SPwu/Bf8" +
        "Avzw+9/70fvE+7v7s/uu+6v7qvus+7D7tvu++8n71vvl+/b7Cvwf/Df8Ufxs/In8qfzJ/Oz8EP02/V39hf2v/dr9Bv4z/mH+kP6/" +
        "/u/+IP9Q/4L/s//l/xUARwB4AKkA2QAJATgBZwGVAcIB7gEYAkICawKSArcC2wL+Ah8DPgNcA3cDkQOpA78D0wPlA/QDAgQNBBYE" +
        "HgQiBCUEJQQkBCAEGgQRBAcE+gPsA9sDyAOzA5wDhANpA00DLwMQA+4CzAKnAoICWwIzAgoC4AG1AYkBXAEvAQEB0gCjAHQARQAW" +
        "AOf/uP+J/1r/LP/+/tH+pf55/k7+JP77/dP9rf2I/WT9Qf0g/QH94/zH/K38lfx+/Gn8VvxG/Df8Kvwf/Bf8EPwM/An8CfwL/A/8" +
        "Ffwd/Cf8M/xC/FL8ZPx4/I78pvy//Nr89/wW/TX9V/16/Z79w/3p/RH+Of5i/oz+t/7j/g//O/9o/5X/wv/w/xwASQB2AKMAzwD7" +
        "ACYBUAF6AaMBywHyARgCPAJgAoICowLCAuAC/AIXAzADRwNdA3ADggOSA6ADrAO2A74DxAPJA8sDywPJA8UDvwO3A60DoQOTA4MD" +
        "cgNeA0kDMwMaAwAD5ALHAqgCiAJnAkUCIQL8AdYBrwGIAWABNwENAeMAuACNAGIANwAMAOH/tv+L/2H/Nv8N/+P+u/6T/mz+Rv4g" +
        "/vz92f23/Zf9eP1a/T39Iv0J/fH82/zH/LT8o/yU/If8fPxy/Gv8Zfxi/GD8YPxi/Gb8bPx0/H38ifyW/KX8tvzJ/N388/wL/ST9" +
        "P/1b/Xj9l/23/dj9+v0e/kL+Z/6N/rP+2/4D/yv/VP99/6b/z//5/yEASgBzAJwAxQDtABQBOwFhAYYBqgHOAfABEgIyAlECbwKL" +
        "AqYCwALYAu4CAwMXAygDOANHA1MDXgNnA24DcwN2A3gDdwN1A3EDawNkA1oDTwNCAzQDIwMRA/4C6ALSAroCoAKFAmkCSwItAg0C" +
        "7AHKAacBhAFfAToBFQHvAMgAoQB6AFIAKwADAN3/tf+O/2f/QP8a//X+0P6r/oj+Zf5D/iL+A/7k/cb9qv2P/XX9Xf1G/TD9HP0K" +
        "/fn86vzd/NH8x/y//Lj8s/yw/K/8r/yy/Lb8u/zD/Mz81/zj/PL8Af0T/Sb9Ov1Q/Wf9gP2a/bX90f3u/Q3+Lf5N/m7+kP6z/tf+" +
        "+/4f/0T/af+P/7X/2/8AACUASwBxAJYAuwDfAAMBJgFJAWsBjAGsAcwB6gEHAiMCPgJYAnECiAKeArICxQLWAuYC9AIBAwwDFgMe" +
        "AyQDKAMrAywDKwMpAyUDHwMYAw8DBQP5AusC3ALLArkCpQKQAnoCYgJJAi8CFAL4AdoBvAGdAX0BXAE7ARkB9wDUALAAjQBpAEUA" +
        "IAD9/9n/tf+R/23/Sv8n/wX/4/7C/qL+gv5k/kb+Kf4N/vL92P3A/aj9kv1+/Wr9WP1I/Tn9K/0f/RX9DP0F/f/8+/z4/Pf8+Pz7" +
        "/P78BP0L/RT9Hv0q/Tf9Rv1W/Wf9ev2P/aT9u/3T/ez9Bv4h/j3+Wv54/pb+tv7W/vb+F/85/1v/ff+f/8L/5P8GACkASwBtAI8A" +
        "sQDSAPMAEwEzAVIBcAGNAaoBxQHgAfkBEgIpAj8CVAJoAnoCiwKbAqkCtgLBAssC1ALbAuAC5ALmAucC5gLjAt8C2gLTAssCwQK1" +
        "AqkCmgKLAnoCaAJUAkACKgITAvsB4gHIAa0BkQF0AVcBOQEaAfsA3AC8AJsAegBZADgAFwD3/9b/tf+U/3T/VP80/xX/9v7Y/rr+" +
        "nv6C/mb+TP4z/hr+A/7t/dj9xP2x/aD9kP2B/XP9Z/1c/VP9S/1F/UD9PP06/Tr9O/09/UH9Rv1N/VX9X/1q/Xb9hP2T/aP9tf3I" +
        "/dz98P0H/h7+Nv5P/mj+g/6f/rv+1/71/hP/Mf9Q/2//jv+t/83/7f8LACsASgBqAIkAqADGAOQAAQEeAToBVQFwAYoBowG7AdIB" +
        "6QH+ARICJQI3AkcCVgJlAnECfQKHApAClwKdAqICpQKnAqcCpgKkAqACmwKUAowCgwJ4AmwCXwJQAkECMAIeAgsC9wHhAcsBtAGc" +
        "AYMBagFQATUBGQH9AOAAwwCmAIgAagBMAC4AEADy/9T/tv+Y/3r/Xf9A/yT/CP/s/tH+t/6e/oX+bf5W/kD+K/4X/gT+8v3h/dH9" +
        "wv21/an9nv2U/Yz9hf2A/Xv9eP13/Xf9eP16/X79g/2K/ZH9mv2l/bD9vf3L/dr96v38/Q7+If42/kv+Yf54/pD+qP7C/tv+9v4R" +
        "/yz/SP9k/4D/nf+6/9f/9P8QAC0ASQBmAIIAngC6ANUA8AAKASQBPQFVAW0BgwGZAa4BwwHWAegB+QEJAhgCJgIyAj4CSAJRAlkC" +
        "YAJlAmkCawJtAm0CbAJpAmYCYQJaAlMCSgJAAjUCKQIbAg0C/QHsAdsByAG0AaABiwF1AV4BRgEuARUB/ADiAMgArgCTAHcAXABA" +
        "ACUACQDu/9P/t/+c/4H/Zv9M/zL/GP///uf+z/64/qH+i/53/mP+T/49/iz+HP4M/v798f3l/dr90P3I/cH9uv22/bL9r/2u/a79" +
        "r/2y/bb9u/3B/cj90f3a/eX98f3+/Qz+G/4r/jz+Tv5g/nT+iP6e/rP+yv7h/vn+Ef8q/0P/XP92/5D/q//F/9//+v8TAC4ASABi" +
        "AHwAlgCvAMgA4AD4AA8BJgE8AVEBZgF6AY0BnwGxAcEB0QHfAe0B+QEEAg8CGAIgAicCLQIxAjUCNwI4AjgCNgI0AjACLAImAh4C" +
        "FgINAgMC9wHrAd0BzwG/Aa8BngGMAXkBZQFRATwBJgEQAfkA4gDLALIAmgCBAGgATwA2AB0AAwDr/9L/uf+g/4f/b/9X/z//KP8R" +
        "//v+5f7Q/rv+qP6V/oP+cf5h/lH+Qv41/ij+HP4R/gf+//33/fH96/3n/eT94v3h/eH94v3l/en97f3z/fr9Av4L/hX+IP4s/jn+" +
        "R/5W/mX+dv6H/pn+rP7A/tT+6P7+/hT/Kv9A/1f/b/+G/57/tv/P/+f///8WAC4ARgBeAHYAjQCkALsA0QDnAPwAEQElATgBSwFd" +
        "AW4BfwGPAZ0BqwG5AcUB0AHaAeMB7AHzAfkB/gECAgUCBwIHAgcCBgIDAgAC+wH1Ae8B5wHeAdUBygG+AbIBpAGWAYcBdwFnAVUB" +
        "QwExAR0BCQH1AOAAywC1AJ8AiQByAFsARAAtABYAAADp/9L/u/+k/43/d/9h/0v/Nv8h/w3/+f7m/tT+wv6x/qD+kP6B/nP+Zv5a" +
        "/k7+Q/46/jH+Kf4i/h3+GP4U/hL+EP4P/hD+Ef4U/hf+HP4h/ij+L/43/kH+S/5W/mL+b/59/ov+m/6r/rv+zf7e/vH+BP8Y/yz/" +
        "QP9V/2r/f/+V/6v/wf/X/+3/AgAYAC4ARABaAHAAhQCaAK8AwwDXAOoA/QAPASEBMgFCAVIBYQFvAX0BiQGVAaABqgGzAbsBwwHJ" +
        "Ac8B0wHXAdkB2wHbAdsB2QHXAdMBzwHJAcMBvAG0AasBoQGWAYsBfgFxAWMBVAFFATUBJAETAQIB7wDcAMkAtgCiAI4AeQBkAE8A" +
        "OgAlABAA/P/n/9L/vf+o/5P/f/9r/1f/RP8x/x//Df/7/ur+2v7K/rv+rf6g/pP+h/58/nH+aP5f/lf+UP5K/kX+Qf4+/jv+Ov46" +
        "/jr+PP4+/kL+Rv5L/lH+WP5g/mn+cv59/oj+lP6g/q7+vP7L/tr+6v76/gv/Hf8v/0H/VP9n/3r/jv+i/7b/yv/e//P/BgAaAC4A" +
        "QgBWAGoAfQCQAKMAtgDIANkA6gD7AAsBGwEpATgBRQFSAV4BagF0AX4BhwGQAZcBnQGjAagBrAGvAbEBsgGyAbIBsAGuAasBpgGh" +
        "AZsBlQGNAYUBewFxAWcBWwFPAUIBNQEnARgBCQH5AOgA2ADGALUAowCRAH4AawBYAEUAMgAeAAsA+f/l/9L/v/+s/5n/h/90/2L/" +
        "Uf9A/y//Hv8P///+8P7i/tX+yP68/rD+pf6b/pL+if6B/nr+dP5v/mr+Z/5k/mL+Yf5h/mH+Y/5l/mj+bP5x/nf+fv6F/o3+lv6g" +
        "/qr+tf7B/s3+2v7o/vb+BP8U/yP/M/9E/1X/Zv93/4n/m/+t/8D/0v/l//f/CQAbAC4AQABSAGQAdgCHAJgAqQC6AMoA2QDoAPcA" +
        "BQESAR8BLAE3AUIBTQFWAV8BZwFvAXUBewGAAYQBiAGKAYwBjQGNAYwBiwGJAYUBgQF9AXcBcQFqAWIBWQFQAUYBPAEwASQBGAEL" +
        "Af0A7wDhANIAwgCyAKIAkgCBAHAAXwBNADwAKgAYAAcA9v/k/9P/wf+w/5//jv99/23/Xf9N/z7/L/8h/xP/Bf/4/uz+4P7V/sv+" +
        "wf64/q/+qP6h/pr+lf6Q/oz+if6H/oX+hP6E/oX+hv6J/oz+kP6U/pr+oP6n/q7+t/6//sn+0/7e/ur+9f4C/w//Hf8r/zn/SP9X" +
        "/2b/dv+G/5b/p/+4/8j/2f/q//v/CwAcAC0APgBOAF8AbwB/AI4AnQCsALsAyQDXAOQA8QD9AAkBFAEfASkBMgE7AUMBSgFQAVYB" +
        "XAFgAWQBZwFpAWoBawFrAWoBaQFnAWQBYAFbAVYBUAFJAUIBOgEyASgBHwEUAQkB/gDyAOUA2ADLAL0ArwCgAJIAggBzAGMAVABE" +
        "ADQAIwATAAMA9P/k/9T/xP+0/6T/lf+G/3f/aP9a/0z/Pv8x/yX/GP8N/wL/9/7t/uT+2/7S/sv+xP6+/rj+s/6v/qv+qf6n/qX+" +
        "pf6l/qb+p/6p/qz+sP60/rn+v/7G/s3+1P7c/uX+7/75/gP/Dv8a/yb/Mv8//0z/Wv9o/3b/hf+T/6L/sf/B/9D/4P/v////DQAd" +
        "ACwAOwBKAFkAaAB2AIUAkwCgAK0AugDHANMA3gDqAPQA/gAIAREBGQEhASgBLwE1AToBPwFDAUYBSQFKAUwBTAFMAUsBSgFHAUUB" +
        "QQE9ATgBMgEsASUBHgEWAQ0BBAH7APEA5gDbAM8AwwC3AKoAnQCQAIIAdQBmAFgASgA7ACwAHgAPAAAA8v/j/9X/xv+4/6n/m/+N" +
        "/4D/c/9m/1n/Tf9B/zX/Kv8g/xb/DP8D//r+8v7r/uT+3v7Y/tP+z/7L/sj+xv7E/sP+wv7D/sP+xf7H/sr+zf7S/tb+3P7i/uj+" +
        "7/73/v/+CP8R/xv/Jf8w/zv/Rv9S/17/a/93/4T/kv+f/63/u//J/9f/5f/z/wAADwAdACsAOQBGAFQAYgBvAHwAiACVAKEArQC4" +
        "AMMAzQDXAOEA6gDzAPsAAwEKARABFgEbASABJAEoASsBLQEuAS8BMAEwAS8BLQErASgBJQEhARwBFwERAQsBBAH9APUA7ADkANoA" +
        "0ADGALwAsQClAJoAjgCBAHUAaABbAE4AQQA0ACYAGQALAP7/8f/j/9b/yf+7/67/ov+V/4n/fP9x/2X/Wv9P/0X/O/8x/yj/H/8X" +
        "/w//CP8B//v+9v7x/uz+6P7l/uL+4P7f/t7+3f7e/t/+4P7i/uX+6P7s/vH+9v77/gH/CP8P/xf/H/8n/zD/Ov9D/07/WP9j/27/" +
        "ev+F/5H/nv+q/7b/w//Q/93/6v/3/wMAEAAdACkANgBDAE8AWwBnAHMAfwCKAJUAoACqALQAvgDHAM8A2ADfAOcA7gD0APoA/wAE" +
        "AQgBDAEPAREBEwEVARYBFgEVARUBEwERAQ4BCwEHAQMB/gD5APMA7QDmAN8A1wDPAMYAvQCzAKoAoACVAIoAfwB0AGkAXQBRAEUA" +
        "OQAtACAAFAAIAPz/8P/k/9f/y/+//7P/qP+c/5H/hv97/3D/Zv9c/1P/Sv9B/zn/Mf8q/yP/HP8W/xH/DP8H/wP/AP/9/vr++f73" +
        "/vf+9v73/vj++f77/v7+Af8F/wn/Df8T/xj/Hv8l/yz/M/87/0T/TP9V/1//aP9y/33/h/+S/53/qP+z/7//y//W/+L/7v/6/wUA" +
        "EQAcACgANAA/AEoAVgBhAGsAdgCAAIoAlACdAKYArwC3AL8AxwDOANQA2gDgAOUA6gDuAPIA9QD4APoA/AD9AP4A/gD+AP0A+wD5" +
        "APcA9ADwAOwA6ADjAN0A1wDRAMoAwwC7ALMAqwCiAJkAkACHAH0AcwBoAF4AUwBIAD0AMgAnABsAEAAFAPr/7//k/9n/zv/D/7j/" +
        "rf+j/5j/jv+F/3v/cv9p/2D/WP9Q/0n/Qf87/zT/L/8p/yT/IP8c/xj/Ff8S/xD/D/8O/w3/Df8O/w//EP8S/xT/F/8b/x//I/8o" +
        "/y3/M/85/z//Rv9O/1X/Xf9l/27/d/+A/4r/k/+d/6f/sv+8/8f/0f/c/+f/8v/8/wYAEQAcACcAMQA8AEYAUABaAGQAbgB3AIAA" +
        "iQCRAJoAoQCpALAAtwC9AMMAyQDOANMA1wDbAN4A4QDjAOUA5wDoAOgA6ADoAOcA5QDjAOEA3gDbANcA0wDOAMkAxAC+ALgAsQCq" +
        "AKMAmwCTAIsAggB5AHAAZwBeAFQASgBAADYALAAhABcADQACAPn/7//k/9r/0P/G/7z/sv+p/6D/lv+N/4X/fP90/2z/Zf9e/1f/" +
        "Uf9L/0X/QP87/zb/Mv8v/yv/Kf8m/yX/I/8i/yL/Iv8i/yP/Jf8n/yn/LP8v/zP/N/87/0D/Rf9L/1H/V/9e/2X/bP90/3z/hP+N" +
        "/5X/nv+n/7H/uv/E/83/1//h/+v/9f///wgAEQAbACUALwA4AEIASwBUAF0AZgBuAHcAfwCGAI4AlQCcAKIAqACuALMAuAC9AMEA" +
        "xQDJAMwAzgDQANIA0wDUANQA1ADUANMA0QDQAM0AywDIAMQAwAC8ALcAsgCtAKcAoQCaAJMAjACFAH0AdgBtAGUAXQBUAEsAQgA5" +
        "ADAAJgAdABMACgAAAPj/7v/l/9z/0v/J/8D/t/+v/6b/nv+W/47/hv9//3j/cf9r/2T/X/9Z/1T/T/9L/0f/Q/9A/z3/O/85/zf/" +
        "Nv81/zX/Nf82/zf/OP86/zz/Pv9B/0X/Sf9N/1H/Vv9b/2H/Z/9t/3T/ev+B/4n/kP+Y/6D/qP+x/7n/wv/L/9P/3P/l/+7/9/8A" +
        "AAkAEgAbACQALAA1AD4ARgBOAFcAXwBmAG4AdQB8AIMAiQCPAJUAmwCgAKUAqgCuALEAtQC4ALsAvQC/AMAAwQDCAMIAwgDCAMEA" +
        "vwC9ALsAuQC2ALMArwCrAKcAogCdAJcAkgCMAIYAfwB4AHEAagBjAFsAUwBLAEMAOwAyACoAIQAZABAABwAAAPf/7v/m/93/1f/M" +
        "/8T/vP+0/6z/pf+e/5b/j/+J/4L/fP92/3H/a/9m/2L/Xv9a/1b/U/9Q/03/S/9J/0j/R/9G/0b/Rv9H/0j/Sf9L/03/UP9S/1b/" +
        "Wf9d/2H/Zv9q/3D/df97/4H/h/+O/5T/m/+i/6r/sf+5/8H/yf/R/9n/4f/p//H/+v8BAAkAEgAaACIAKgAyADoAQgBJAFEAWABf" +
        "AGYAbABzAHkAfwCEAIoAjwCTAJgAnACfAKMApgCpAKsArQCvALAAsQCxALIAsQCxALAArwCtAKsAqQCmAKMAnwCcAJgAkwCPAIoA" +
        "hAB/AHkAcwBtAGYAYABZAFIASwBDADwANAAtACUAHQAVAA0ABQD+//b/7v/n/9//1//Q/8j/wf+5/7L/q/+l/57/mP+S/4z/hv+B" +
        "/3z/d/9z/2//a/9n/2T/Yf9e/1z/Wv9Z/1j/V/9W/1b/Vv9X/1j/Wf9b/13/X/9i/2X/aP9s/2//dP94/33/gv+H/43/k/+Z/5//" +
        "pf+s/7L/uf/A/8f/z//W/97/5f/t//T//P8CAAoAEQAZACAAKAAvADYAPQBEAEsAUQBYAF4AZABqAG8AdQB6AH8AgwCHAIsAjwCS" +
        "AJUAmACbAJ0AnwCgAKEAogCiAKIAogCiAKEAnwCeAJwAmgCXAJQAkQCOAIoAhgCBAH0AeABzAG4AaABiAFwAVgBQAEoAQwA8ADUA" +
        "LgAnACAAGQASAAsAAwD9//b/7//n/+D/2f/S/8z/xf++/7j/sv+r/6b/oP+a/5X/kP+L/4f/gv9+/3r/d/90/3H/bv9s/2r/aP9n" +
        "/2b/Zf9l/2X/Zf9m/2f/aP9p/2v/bf9w/3P/dv95/33/gP+F/4n/jv+T/5j/nf+i/6j/rv+0/7r/wf/H/87/1P/b/+L/6f/w//f/" +
        "/f8DAAoAEQAYAB8AJgAsADMAOQA/AEYATABRAFcAXABiAGcAbABwAHUAeQB8AIAAgwCGAIkAjAA=";

    private static void WriteWavStereo(string path, short[] samples, int sampleRate = 44100)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);

        int channels = 2;
        int byteRate = sampleRate * channels * 2;
        int dataSize = samples.Length * 2;

        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)(channels * 2));
        bw.Write((short)16);
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);

        for (int i = 0; i < samples.Length; i++)
        {
            bw.Write(samples[i]);
        }
    }

    private static void WriteWav(string path, short[] samples, int sampleRate = 44100)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var bw = new BinaryWriter(fs);

        int channels = 1;
        int byteRate = sampleRate * channels * 2;
        int dataSize = samples.Length * 2;

        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)(channels * 2));
        bw.Write((short)16);
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);

        for (int i = 0; i < samples.Length; i++)
        {
            bw.Write(samples[i]);
        }
    }
}
