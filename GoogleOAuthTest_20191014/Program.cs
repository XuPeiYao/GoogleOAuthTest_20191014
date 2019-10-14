using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleOAuthTest_20191014
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                new Program().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }

            Console.ReadKey();
        }

        private async Task Run()
        {
            UserCredential credential;

            #region 打補釘
            var harmony = HarmonyInstance.Create("test.xpy");
            var originalOpenBrowserMethod = typeof(LocalServerCodeReceiver)
                .GetMethod("OpenBrowser", BindingFlags.Instance | BindingFlags.NonPublic);
            var newOpenBrowserMethod = typeof(Program)
                .GetMethod("Transpiler", BindingFlags.Static | BindingFlags.NonPublic);
            harmony.Patch(originalOpenBrowserMethod, transpiler: new HarmonyMethod(newOpenBrowserMethod));
            #endregion

            Console.WriteLine("打補釘完成，強制使用IE");

            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { "https://www.googleapis.com/auth/userinfo.profile" },
                    "user",
                    CancellationToken.None,
                    new NullDataStore(),
                    new LocalServerCodeReceiver(File.ReadAllText("closePage.html"))
                );
            }

            Console.WriteLine("Google登入完成，IE將自動關閉");
        }

        private static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
        {
            // 異動原有SDK的OpenBrowser方法的IL Code
            var result = instructions.ToList();

            // 在堆疊前端加入字串參數
            result.Insert(0, new CodeInstruction(OpCodes.Ldstr, "IExplore.exe"));

            // 替換調用方法為兩個參數的Process.Start
            var start = typeof(Process).GetMethod("Start", new Type[] { typeof(string), typeof(string) });
            result[2] = new CodeInstruction(OpCodes.Call, start);

            return result;
        }
    }
}
