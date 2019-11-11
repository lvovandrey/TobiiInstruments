﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TobiiParser
{
    public class TobiiCsvReader
    {
        public List<TobiiRecord> tobiiList;
        public List<TobiiRecord> FiltredTobiiList;

        public void TobiiCSCRead(string filename, List<TobiiRecord> tobiiList)
        {
            
            char separator = '\n';
            char delimiter = '\t';

            int N_timestampCol = 0, N_firstZoneCol = 0;
            int ZoneColCount = 10;
            long i = 0;
            using (StreamReader rd = new StreamReader(new FileStream(filename, FileMode.Open)))
            {
                string[] first_string_arr = { "" };
                first_string_arr = rd.ReadLine().Split(delimiter);
                N_timestampCol = SearchCol(first_string_arr, "Recording timestamp");
                N_firstZoneCol = SearchCol(first_string_arr, "AOI hit [");

                bool EndOfFile=false;
                while (!EndOfFile)
                {
                    string[] str_arr = { "" };
                    string big_str = "";
                    EndOfFile = ReadPartOfFile(rd, out big_str);

                    str_arr = big_str.Split(separator);
                    foreach (string s in str_arr)
                    {
                        string[] tmp = { "" };
                        i++;
                        tmp = s.Split(delimiter);
                        if (tmp.Count() < 3) continue;
                        TobiiRecord TR = new TobiiRecord();
                        if (!long.TryParse(tmp[N_timestampCol], out TR.time_ms))
                            throw new Exception("Не могу преобразовать в timestamp строку  " + tmp[N_timestampCol]);

                        string[] Hits = new string[tmp.Count()];
                        Array.Copy(tmp, N_firstZoneCol, Hits, 0, ZoneColCount);
                        TR.zone = SearchCol(Hits, "1"); if (TR.zone != -1) TR.zone++;
                        tobiiList.Add(TR);
                    }

                }

                FiltredTobiiList = CompactTobiiRecords(tobiiList);
            }





        }

        bool ReadPartOfFile(StreamReader rd, out string str)
        {
            bool endOfFile = false;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= 10000; i++)
            {
                string s = rd.ReadLine();
                if (s == null) { endOfFile = true; break;  }
                sb.Append(s);
                sb.Append("\n");
            }
            str = sb.ToString();
            return endOfFile;
        }


        int SearchCol(string[] row, string colName)
        {
            int ii = 0;
            bool find = false;
            foreach (string s in row)
            {
                if (s == null) continue;
                if (s.IndexOf(colName) > -1)
                { find = true; break; }
                ii++;
            }
            if (find) return ii;
            else { return -1; }
        }

        //Убираем повторы из записи тоби - компактифицируем ее
        public List<TobiiRecord> CompactTobiiRecords(List<TobiiRecord> tRs)
        {
            List<TobiiRecord> TRSNew = new List<TobiiRecord>();
            int ZoneBefore = -2;
            foreach (var tr in tRs)
            {
                if (tr.zone != ZoneBefore)
                {
                    TRSNew.Add(tr);
                    ZoneBefore = tr.zone;
                }
            }
            return TRSNew;
        }

       public List<TobiiRecord> ClearFromGarbageZone(List<TobiiRecord> tRs, int GarbageZone, long UPBoundFiltrationOfGarbage)
        {
            List<TobiiRecord> TRSNew = new List<TobiiRecord>();
            foreach (var tr in tRs)
                if (tr.zone != GarbageZone || tRs.IndexOf(tr) == 0 || tRs.IndexOf(tr) > tRs.Count - 2 || (tRs[tRs.IndexOf(tr) + 1].time_ms - tr.time_ms > UPBoundFiltrationOfGarbage))
                    TRSNew.Add(tr);

            return TRSNew;
        }

    }
}
