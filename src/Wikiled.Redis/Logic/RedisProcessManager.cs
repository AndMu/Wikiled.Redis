using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NLog;

namespace Wikiled.Redis.Logic
{
    public class RedisProcessManager : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly int port;

        private readonly string configuration;

        private Process process;

        private bool hasStarted;

        public RedisProcessManager(int? port = null, string configurationFile = null)
        {
            this.port = port ?? 6666;
            configuration = configurationFile;
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!hasStarted)
            {
                return;
            }

            log.Info("Redis was started for this test run. Shuting down");
            if (process != null)
            {
                if (process.HasExited)
                {
                    log.Error("Process already existed");
                }
                else
                {
                    if (!process.CloseMainWindow())
                    {
                        log.Info("Close failed");
                        process.Kill();
                    }
                    else
                    {
                        process.WaitForExit();
                    }
                }
            }
            else
            {
                log.Error("Process is null");
            }
        }

        public void Start(string root)
        {
            log.Info("Checking Redis instance");

            var redisPath = ConfigurationManager.AppSettings["Redis"];

            if (string.IsNullOrEmpty(redisPath))
            {
                throw new NullReferenceException("Redis configuration not found");
            }

            redisPath = Path.Combine(root, redisPath);
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.Arguments = $"{configuration} --port {port}";
            startInfo.WorkingDirectory = redisPath;
            startInfo.FileName = Path.GetFullPath(Path.Combine(redisPath, "redis-server.exe"));
            startInfo.CreateNoWindow = false;
            process = new Process();
            process.StartInfo = startInfo;
            if (!process.Start())
            {
                throw new NullReferenceException("Redis startup failed");
            }
            
            hasStarted = true;
            Thread.Sleep(2000);
            if (process.HasExited)
            {
                throw new NullReferenceException($"Redis startup failed, there maybe another process using the same port {port}");
            }
        }
    }
}