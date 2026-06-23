using System;
using System.Collections.Generic;
using System.IO;

namespace AtsPluginNs
{
    /// <summary>
    /// 1駅分のデータを表す構造体
    /// </summary>
    public class TimsStationData
    {
        public string StationName;     // 駅名 (例: "新浜空港")
        public int ArrivalTime;        // 着時刻 (生のCSV値。例: 72100)
        public int DepartureTime;      // 発時刻 (生のCSV値)
        public string Track;           // 番線名
        public double DistanceKm;      // 駅間キロ
        public int MaxSpeed;           // 最高速度
        public int StationNumber;      // 駅番号 (地上子1001の値と対応)
    }

    /// <summary>
    /// 仕業カードCSV(例: 3621M.csv)を読み込み、駅情報のリストを保持するクラス
    /// </summary>
    public class TimsDataLoader
    {
        public string DepotName { get; private set; }      // 所属(運輸区)
        public string TrainNumber { get; private set; }    // 列車番号 (例: "3621M")
        public List<TimsStationData> Stations { get; private set; }

        public TimsDataLoader()
        {
            Stations = new List<TimsStationData>();
        }

        /// <summary>
        /// CSVファイルを読み込む。失敗した場合は何もしない(例外を投げない)。
        /// </summary>
        public bool Load(string csvFilePath)
        {
            try
            {
                if (!File.Exists(csvFilePath))
                {
                    return false;
                }

                Stations.Clear();
                string[] lines = File.ReadAllLines(csvFilePath, System.Text.Encoding.GetEncoding("Shift_JIS"));

                foreach (string rawLine in lines)
                {
                    if (string.IsNullOrWhiteSpace(rawLine)) continue;

                    string[] cols = rawLine.Split(',');
                    if (cols.Length == 0) continue;

                    string rowType = cols[0].Trim();

                    // 行頭の番号によって意味が変わる
                    switch (rowType)
                    {
                        case "0":
                            // 所属(運輸区)
                            if (cols.Length > 1) DepotName = cols[1].Trim();
                            break;

                        case "3":
                            // 列車番号
                            if (cols.Length > 1) TrainNumber = cols[1].Trim();
                            break;

                        case "7":
                            // 駅情報行
                            ParseStationRow(cols);
                            break;

                        // case "5": 種別/行先ブロックなど、必要になったら追加する
                    }
                }

                return true;
            }
            catch (Exception)
            {
                // 読み込み失敗時はクラッシュさせず、falseを返すだけにする
                return false;
            }
        }

        private void ParseStationRow(string[] cols)
        {
            try
            {
                // 列の並び: 7,運転時分,駅名,着時刻,発時刻,番線名,進入制限,進出制限,停目キロ,断,駅間キロ,音声,線,駅,CH,最小線区,最高速度,運転方向,駅番号
                // ※実際の列インデックスはCSVの内容を見ながら調整が必要

                var station = new TimsStationData();

                station.StationName = cols.Length > 2 ? cols[2].Trim() : "";
                station.ArrivalTime = ParseIntSafe(cols, 3);
                station.DepartureTime = ParseIntSafe(cols, 4);
                station.Track = cols.Length > 5 ? cols[5].Trim() : "";
                station.DistanceKm = ParseDoubleSafe(cols, 10);
                station.MaxSpeed = ParseIntSafe(cols, 16);
                station.StationNumber = ParseIntSafe(cols, 18);

                Stations.Add(station);
            }
            catch (Exception)
            {
                // 1行のパースに失敗しても、他の行の処理は続ける
            }
        }

        private int ParseIntSafe(string[] cols, int index)
        {
            if (index >= cols.Length) return -1;
            int result;
            if (int.TryParse(cols[index].Trim(), out result))
            {
                return result;
            }
            return -1;
        }

        private double ParseDoubleSafe(string[] cols, int index)
        {
            if (index >= cols.Length) return -1;
            double result;
            if (double.TryParse(cols[index].Trim(), out result))
            {
                return result;
            }
            return -1;
        }

        /// <summary>
        /// 駅番号(地上子1001の値)から、対応する駅データを取得する
        /// </summary>
        public TimsStationData GetStationByNumber(int stationNumber)
        {
            foreach (var s in Stations)
            {
                if (s.StationNumber == stationNumber)
                {
                    return s;
                }
            }
            return null;
        }

        /// <summary>
        /// $ICCardNumberの値(例: 362108)から、対応するCSVファイル名を組み立てる
        /// 例: 362108 -> "3621M.csv"
        /// </summary>
        public static string BuildCsvFileName(int icCardNumber)
        {
            // 先頭4桁を列車番号として取り出す
            // 362108 -> "362108" を文字列化し、末尾2桁を除いた部分を使う
            string s = icCardNumber.ToString();
            if (s.Length <= 2) return null;

            string trainNumberPart = s.Substring(0, s.Length - 2);
            return trainNumberPart + "M.csv";
        }
    }
}
