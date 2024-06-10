﻿

using Afdian.Action.ViewModels;
using Afdian.Sdk;
using RazorEngineCore;

namespace Afdian.Action
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, Afdian.Action!");

            string githubWorkspace = Utils.GitHubActionsUtil.GitHubEnv(Utils.GitHubActionsUtil.GitHubEnvKeyEnum.GITHUB_WORKSPACE);
            Console.WriteLine($"GITHUB_WORKSPACE: {githubWorkspace}");
            bool githubAction = true;
            if (githubWorkspace == null)
            {
                Console.WriteLine("非 GitHub Action 环境");
                githubAction = false;
            }
            // TODO: 暂不支持 定义 多个模板文件 对应 多个目标文件, 如果需要修改多个文件, 则创建多个 afdian-action.yml

            // 传进来的时候 就已经是加上 githubWorkspace 的绝对路径

            string templateFilePath = Utils.GitHubActionsUtil.GetEnv("template_filePath");
            string afdianUserId = Utils.GitHubActionsUtil.GetEnv("afdian_userId");
            string afdianToken = Utils.GitHubActionsUtil.GetEnv("afdian_token");
            string targetFilePath = Utils.GitHubActionsUtil.GetEnv("target_filePath");
            string startFlag = Utils.GitHubActionsUtil.GetEnv("start_flag") ?? "<!-- AFDIAN-ACTION:START -->";
            string endFlag = Utils.GitHubActionsUtil.GetEnv("end_flag") ?? "<!-- AFDIAN-ACTION:END -->";
            string usingsStr = Utils.GitHubActionsUtil.GetEnv("usings") ?? "";
            string assemblyReferencesStr = Utils.GitHubActionsUtil.GetEnv("assemblyReferences") ?? "";

            if (!githubAction)
            {
                templateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Views/Test.cshtml");
                targetFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Views/Test.md");
                // 注意: 不要提交上去了
                afdianUserId = "";
                afdianToken = "";
            }

            // 1. 获取爱发电数据
            AfdianClient afdianClient = new AfdianClient(userId: afdianUserId, token: afdianToken);
            string pingJsonStr = afdianClient.Ping();
            if (!pingJsonStr.Contains("200"))
            {
                string message = "user_id, token 效验不通过";
                Utils.LogUtil.Error(message);
                throw new Exception("运行失败");
            }
            var orderModel = afdianClient.QueryOrderModel();
            var sponsorModel = afdianClient.QuerySponsorModel();

            // 2. 组成 ViewModel
            var viewModel = new ViewModels.AfdianViewModel()
            {
                Order = orderModel,
                Sponsor = sponsorModel
            };

            // 3. ViewModel 传递给 afdian-action-template.cshtml (templateFilePath), RazorEngine 解析
            IRazorEngine razorEngine = new RazorEngine();
            string templateText = "";
            string runResult = "";
            try
            {
                templateText = Utils.FileUtil.ReadStringAsync(templateFilePath).Result;
            }
            catch (Exception ex)
            {
                Utils.LogUtil.Error("模板文件 读取 失败");
                Utils.LogUtil.Exception(ex);
                throw new Exception("运行失败");
            }
            if (string.IsNullOrEmpty(templateText))
            {
                Utils.LogUtil.Error("模板文件 不能为空");
                throw new Exception("运行失败");
            }
            try
            {
                #region assemblyReference, using
                List<string> assemblyReferenceList = new List<string>()
                {
                    "System.Collections, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
                };
                var assemblyReferenceTemp = assemblyReferencesStr.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (assemblyReferenceTemp != null && assemblyReferenceTemp.Length >= 1)
                {
                    assemblyReferenceList.AddRange(assemblyReferenceTemp);
                }
                assemblyReferenceList = assemblyReferenceList.Distinct().ToList();
                List<string> usingList = new List<string>()
                {
                    "System.Collections.Generic",
                    "System.Linq",
                };
                var usingsTemp = usingsStr.Split(new string[] { ";", " " }, StringSplitOptions.RemoveEmptyEntries);
                if (usingsTemp != null && usingsTemp.Length >= 1)
                {
                    usingList.AddRange(usingsTemp);
                }
                usingList = usingList.Distinct().ToList();
                #endregion

                IRazorEngineCompiledTemplate<RazorEngineTemplateBase<AfdianViewModel>> template =
                    razorEngine.Compile<RazorEngineTemplateBase<AfdianViewModel>>(templateText, builder =>
                    {
                        builder.AddAssemblyReference(typeof(System.IO.File)); // by type
                        builder.AddAssemblyReference(typeof(Afdian.Sdk.ResponseModels.QuerySponsorResponseModel)); // by type

                        foreach (var item in assemblyReferenceList)
                        {
                            builder.AddAssemblyReferenceByName(item);
                        }
                        foreach (var item in usingList)
                        {
                            builder.AddUsing(item);
                        }
                    });

                runResult = template.Run(instance =>
                {
                    instance.Model = viewModel;
                });
            }
            catch (Exception ex)
            {
                Utils.LogUtil.Error("模板文件 编译/运行 失败");
                Utils.LogUtil.Exception(ex);
                throw new Exception("运行失败");
            }
            if (string.IsNullOrEmpty(runResult))
            {
                Utils.LogUtil.Error("运行结果为 空");
                throw new Exception("运行失败");
            }

            // 4. 找到 目标文件 -> 根据开始结束符号 -> 找到修改插入位置 -> 修改文件内容
            try
            {
                bool existTargetFile = File.Exists(targetFilePath);
                if (!existTargetFile)
                {
                    Utils.LogUtil.Error("不存在目标文件");
                    throw new Exception("运行失败");
                }
                string targetFileContent = Utils.FileUtil.ReadStringAsync(targetFilePath).Result;
                int startFlagIndex = targetFileContent.IndexOf(startFlag);
                int endFlagIndex = targetFileContent.IndexOf(endFlag);
                if (startFlagIndex != -1 && endFlagIndex != -1 && endFlagIndex > startFlagIndex)
                {
                    string beforeStr = targetFileContent.Substring(0, startFlagIndex);
                    string afterSstr = targetFileContent.Substring(endFlagIndex + endFlag.Length);
                    string curTime = $"\n<!-- update time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} -->\n";
                    string centerStr = startFlag + curTime + runResult + "\n" + endFlag;
                    targetFileContent = beforeStr + centerStr + afterSstr;

                    File.WriteAllText(targetFilePath, targetFileContent, System.Text.Encoding.UTF8);

                    Console.WriteLine("更新完成");
                }
            }
            catch (Exception ex)
            {
                Utils.LogUtil.Error("更新 目标文件 失败");
                Utils.LogUtil.Exception(ex);
                throw new Exception("运行失败");
            }

        }
    }
}