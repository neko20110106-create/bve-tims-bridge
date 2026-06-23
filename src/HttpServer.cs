using System;
using System.Net;
using System.Text;
using System.Threading;

namespace AtsPluginNs
{
    /// <summary>
    /// 簡易HTTPサーバー。最新のTIMSデータ(JSON文字列)を保持し、
    /// リクエストが来たらそれをそのまま返す。
    /// </summary>
    public static class HttpServer
    {
        private static HttpListener listener;
        private static Thread serverThread;
        private static volatile bool running = false;

        // 現在のTIMSデータ(JSON文字列)。Elapse()から毎フレーム更新される。
        private static volatile string latestJson = "{}";

        public static void UpdateData(string json)
        {
            latestJson = json;
        }

        public static void Start(int port = 8080)
        {
            if (running) return;

            try
            {
                listener = new HttpListener();
                // ローカルのすべてのアドレスからアクセスを受け付ける
                listener.Prefixes.Add($"http://+:{port}/");
                listener.Start();
                running = true;

                serverThread = new Thread(ListenLoop);
                serverThread.IsBackground = true;
                serverThread.Start();
            }
            catch (Exception)
            {
                // ポート使用中、権限不足などで失敗した場合は何もしない
                // (BVE本体をクラッシュさせないため、例外は握りつぶす)
                running = false;
            }
        }

        public static void Stop()
        {
            running = false;
            try
            {
                listener?.Stop();
                listener?.Close();
            }
            catch (Exception)
            {
                // 終了処理中の例外は無視する
            }
        }

        private static void ListenLoop()
        {
            while (running)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext(); // ここでブロックする
                    HandleRequest(context);
                }
                catch (Exception)
                {
                    // GetContext中にStop()が呼ばれると例外が出るが無視してループを抜ける
                    if (!running) break;
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                HttpListenerResponse response = context.Response;

                // CORS対応: iPad(Safari)からfetchで読めるようにする
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.ContentType = "application/json; charset=utf-8";

                byte[] buffer = Encoding.UTF8.GetBytes(latestJson);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception)
            {
                // 個別リクエストの処理失敗はサーバー全体を落とさないよう無視する
            }
        }
    }
}
