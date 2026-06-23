using System;
using System.Runtime.InteropServices;
using System.Text;
using RGiesecke.DllExport;

namespace AtsPluginNs
{
    public class AtsPlugin
    {
        // ----- 構造体定義 -----

        [StructLayout(LayoutKind.Sequential)]
        public struct ATS_VEHICLESPEC
        {
            public int BrakeNotches;
            public int PowerNotches;
            public int AtsNotch;
            public int B67Notch;
            public int Cars;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ATS_VEHICLESTATE
        {
            public double Location;
            public float Speed;
            public int Time;      // ミリ秒。86400000で割った余りが「その日の経過ミリ秒」
            public float BcPressure;
            public float MrPressure;
            public float ErPressure;
            public float BpPressure;
            public float SapPressure;
            public float Current;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ATS_BEACONDATA
        {
            public int Type;
            public int Signal;
            public float Distance;
            public int Optional;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ATS_HANDLES
        {
            public int Brake;
            public int Power;
            public int Reverser;
            public IntPtr ConstantSpeed;
        }

        // ----- プラグイン内部状態 -----

        // 仕業カードCSVが入っているフォルダ。実際の配置に合わせて後で調整する。
        private static readonly string IcCardFolder = @"Plugin\IC\Niihama";

        private static TimsDataLoader timsData = new TimsDataLoader();
        private static int currentTrainNumber = -1;       // $ICCardNumberの生値
        private static int currentStationNumber = -1;      // 地上子1001の値
        private static float currentSpeed = 0;
        private static int currentTimeMs = 0;

        // ----- プラグイン基本情報 -----

        [DllExport("Load", CallingConvention = CallingConvention.StdCall)]
        public static void Load()
        {
            HttpServer.Start(8080);
        }

        [DllExport("Dispose", CallingConvention = CallingConvention.StdCall)]
        public static void Dispose()
        {
            HttpServer.Stop();
        }

        [DllExport("GetPluginVersion", CallingConvention = CallingConvention.StdCall)]
        public static int GetPluginVersion()
        {
            return 131072;
        }

        [DllExport("SetVehicleSpec", CallingConvention = CallingConvention.StdCall)]
        public static void SetVehicleSpec(ATS_VEHICLESPEC spec)
        {
        }

        [DllExport("Initialize", CallingConvention = CallingConvention.StdCall)]
        public static void Initialize(int brake)
        {
            // シナリオ開始時。まだ列車番号が来ていないのでCSVはここでは読まない。
            currentTrainNumber = -1;
            currentStationNumber = -1;
        }

        [DllExport("Elapse", CallingConvention = CallingConvention.StdCall)]
        public static ATS_HANDLES Elapse(ATS_VEHICLESTATE state, IntPtr panel, IntPtr sound)
        {
            currentSpeed = state.Speed;
            currentTimeMs = state.Time;

            PublishJson();

            ATS_HANDLES handles = new ATS_HANDLES();
            handles.Brake = -1;
            handles.Power = -1;
            handles.Reverser = -1;
            handles.ConstantSpeed = IntPtr.Zero;
            return handles;
        }

        [DllExport("SetPower", CallingConvention = CallingConvention.StdCall)]
        public static void SetPower(int notch) { }

        [DllExport("SetBrake", CallingConvention = CallingConvention.StdCall)]
        public static void SetBrake(int notch) { }

        [DllExport("SetReverser", CallingConvention = CallingConvention.StdCall)]
        public static void SetReverser(int pos) { }

        [DllExport("KeyDown", CallingConvention = CallingConvention.StdCall)]
        public static void KeyDown(int key) { }

        [DllExport("KeyUp", CallingConvention = CallingConvention.StdCall)]
        public static void KeyUp(int key) { }

        [DllExport("HornBlow", CallingConvention = CallingConvention.StdCall)]
        public static void HornBlow(int type) { }

        [DllExport("DoorOpen", CallingConvention = CallingConvention.StdCall)]
        public static void DoorOpen()
        {
        }

        [DllExport("DoorClose", CallingConvention = CallingConvention.StdCall)]
        public static void DoorClose()
        {
        }

        [DllExport("SetSignal", CallingConvention = CallingConvention.StdCall)]
        public static void SetSignal(int signal) { }

        [DllExport("SetBeaconData", CallingConvention = CallingConvention.StdCall)]
        public static void SetBeaconData(ATS_BEACONDATA beacon)
        {
            try
            {
                if (beacon.Type == 1000)
                {
                    // 列車番号(IC番号)を受信 -> 対応するCSVを読み込む
                    currentTrainNumber = beacon.Optional;
                    LoadCsvForTrainNumber(currentTrainNumber);
                }
                else if (beacon.Type == 1001)
                {
                    // 駅番号を受信
                    currentStationNumber = beacon.Optional;
                }
            }
            catch (Exception)
            {
                // 地上子処理の失敗でBVE本体を落とさない
            }
        }

        private static void LoadCsvForTrainNumber(int icCardNumber)
        {
            string fileName = TimsDataLoader.BuildCsvFileName(icCardNumber);
            if (fileName == null) return;

            string fullPath = System.IO.Path.Combine(IcCardFolder, fileName);
            timsData.Load(fullPath);
        }

        // ----- JSON生成・送信 -----

        private static void PublishJson()
        {
            try
            {
                string trainNumberStr = timsData.TrainNumber ?? "----";

                TimsStationData currentStation = timsData.GetStationByNumber(currentStationNumber);
                string stationName = currentStation != null ? currentStation.StationName : "";
                int arrivalTime = currentStation != null ? currentStation.ArrivalTime : -1;
                int departureTime = currentStation != null ? currentStation.DepartureTime : -1;

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"trainNumber\":\"").Append(JsonEscape(trainNumberStr)).Append("\",");
                sb.Append("\"speed\":").Append(currentSpeed.ToString("F1")).Append(",");
                sb.Append("\"timeMs\":").Append(currentTimeMs).Append(",");
                sb.Append("\"stationNumber\":").Append(currentStationNumber).Append(",");
                sb.Append("\"stationName\":\"").Append(JsonEscape(stationName)).Append("\",");
                sb.Append("\"arrivalTime\":").Append(arrivalTime).Append(",");
                sb.Append("\"departureTime\":").Append(departureTime);
                sb.Append("}");

                HttpServer.UpdateData(sb.ToString());
            }
            catch (Exception)
            {
                // JSON生成失敗時もBVE本体を落とさない
            }
        }

        private static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
