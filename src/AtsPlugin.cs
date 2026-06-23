using System;
using System.Runtime.InteropServices;
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

        // ----- プラグイン基本情報 -----

        [DllExport("Load", CallingConvention = CallingConvention.StdCall)]
        public static void Load()
        {
            // プラグインがBVEに読み込まれた時に呼ばれる
            // 今後ここでWebSocketサーバーを起動する予定
        }

        [DllExport("Dispose", CallingConvention = CallingConvention.StdCall)]
        public static void Dispose()
        {
            // プラグインがBVEから解放される時に呼ばれる
            // 今後ここでWebSocketサーバーを停止する予定
            // クラッシュ防止のため、必ず実装すること
        }

        [DllExport("GetPluginVersion", CallingConvention = CallingConvention.StdCall)]
        public static int GetPluginVersion()
        {
            return 131072; // バージョン情報。固定値でOK
        }

        [DllExport("SetVehicleSpec", CallingConvention = CallingConvention.StdCall)]
        public static void SetVehicleSpec(ATS_VEHICLESPEC spec)
        {
            // 車両データ読み込み時に呼ばれる
        }

        [DllExport("Initialize", CallingConvention = CallingConvention.StdCall)]
        public static void Initialize(int brake)
        {
            // シナリオ開始・始発駅復帰時に呼ばれる
            // 今後ここで列車番号・行路情報などの設定ファイルを読み込む予定
        }

        [DllExport("Elapse", CallingConvention = CallingConvention.StdCall)]
        public static ATS_HANDLES Elapse(ATS_VEHICLESTATE state, IntPtr panel, IntPtr sound)
        {
            // 毎フレーム呼ばれる。速度・時刻などはここで取得できる
            // 今後ここでWebSocketへ送信する予定

            ATS_HANDLES handles = new ATS_HANDLES();
            handles.Brake = -1;       // -1は「変更なし」を意味する
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
            // ドアが開いた時に呼ばれる
        }

        [DllExport("DoorClose", CallingConvention = CallingConvention.StdCall)]
        public static void DoorClose()
        {
            // ドアが閉まった時に呼ばれる
        }

        [DllExport("SetSignal", CallingConvention = CallingConvention.StdCall)]
        public static void SetSignal(int signal) { }

        [DllExport("SetBeaconData", CallingConvention = CallingConvention.StdCall)]
        public static void SetBeaconData(ATS_BEACONDATA beacon)
        {
            // 地上子を踏んだ時に呼ばれる
        }
    }
}
