using System.Collections.Generic;
using UnityEngine;

namespace Ulak.Core
{
    /// <summary>Tek replik: kim söylüyor + ne söylüyor.</summary>
    public struct Replik
    {
        public string Konusan;
        public string Metin;
        public Replik(string konusan, string metin) { Konusan = konusan; Metin = metin; }
    }

    /// <summary>
    /// replikler.txt dosyasını bölümlere ayırıp "KONUŞAN: metin" satırlarını çıkarır.
    /// Bölüm başlıkları "BÖLÜM N" kalıbıyla bulunur; paragraf/sahne yönergeleri
    /// (parantezle başlayan ya da iki nokta içermeyen satırlar) atlanır.
    /// </summary>
    public static class ReplikDeposu
    {
        private static Dictionary<int, List<Replik>> _bolumler;

        public static List<Replik> Bolum(TextAsset kaynak, int no)
        {
            if (_bolumler == null) Parse(kaynak != null ? kaynak.text : "");
            return _bolumler.TryGetValue(no, out var liste) ? liste : null;
        }

        private static void Parse(string metin)
        {
            _bolumler = new Dictionary<int, List<Replik>>();
            int aktif = -1;

            foreach (var ham in metin.Split('\n'))
            {
                string satir = ham.Trim();
                if (satir.Length == 0 || satir.StartsWith("//")) continue;

                // Bölüm başlığı?
                int ix = satir.IndexOf("BÖLÜM ", System.StringComparison.OrdinalIgnoreCase);
                if (ix < 0) ix = satir.IndexOf("BOLUM ", System.StringComparison.OrdinalIgnoreCase);
                if (ix >= 0)
                {
                    string sonra = satir.Substring(ix + 6).TrimStart();
                    int n = 0, k = 0;
                    while (k < sonra.Length && char.IsDigit(sonra[k])) { n = n * 10 + (sonra[k] - '0'); k++; }
                    if (k > 0) { aktif = n; if (!_bolumler.ContainsKey(n)) _bolumler[n] = new List<Replik>(); }
                    continue;
                }

                if (aktif < 0) continue;
                if (satir.StartsWith("(")) continue; // sahne yönergesi

                // "KONUŞAN: metin" kalıbı
                int sep = satir.IndexOf(':');
                if (sep <= 0 || sep > 24) continue;
                string konusan = satir.Substring(0, sep).Trim().TrimEnd(' ');
                string replik = satir.Substring(sep + 1).Trim();
                if (replik.Length == 0 || konusan.Contains("(")) continue;

                _bolumler[aktif].Add(new Replik(NormalizeKonusan(konusan), replik));
            }
        }

        /// <summary>Yazım varyasyonlarını tek isme indirger (tonykuk → TONYUKUK vb.).</summary>
        private static string NormalizeKonusan(string s)
        {
            string u = s.ToUpperInvariant().Replace("İ", "I");
            if (u.Contains("TONY")) return "TONYUKUK";
            if (u.Contains("BUM")) return "BUMIN";
            if (u.Contains("BALAMIR")) return "BALAMIR";
            if (u.Contains("KAM")) return "KAM";
            if (u.Contains("ASKER")) return "ASKER";
            if (u.Contains("GULYABAN")) return "GULYABANI";
            return u;
        }
    }
}
