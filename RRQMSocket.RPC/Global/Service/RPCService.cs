//------------------------------------------------------------------------------
//  此代码版权归作者本人若汝棋茗所有
//  源代码使用协议遵循本仓库的开源协议及附加协议，若本仓库没有设置，则按MIT开源协议授权
//  CSDN博客：https://blog.csdn.net/qq_40374647
//  哔哩哔哩视频：https://space.bilibili.com/94253567
//  Gitee源代码仓库：https://gitee.com/RRQM_Home
//  Github源代码仓库：https://github.com/RRQM
//  交流QQ群：234762506
//  感谢您的下载和使用
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
using RRQMCore;
using RRQMCore.Exceptions;
using RRQMCore.Helper;
using RRQMSocket.RPC.RRQMRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RRQMSocket.RPC
{
    /// <summary>
    /// RPC服务器类
    /// </summary>
    public class RPCService : IDisposable
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RPCService()
        {
            this.ServerProviders = new ServerProviderCollection();
            this.RPCParsers = new RPCParserCollection();
            this.MethodMap = new MethodMap();
        }

        /// <summary>
        /// 获取函数映射图实例
        /// </summary>
        public MethodMap MethodMap { get; private set; }

        /// <summary>
        /// 获取RPC解析器集合
        /// </summary>
        public RPCParserCollection RPCParsers { get; private set; }

        /// <summary>
        /// 服务实例集合
        /// </summary>
        public ServerProviderCollection ServerProviders { get; private set; }

        /// <summary>
        /// 添加RPC解析器
        /// </summary>
        /// <param name="key">名称</param>
        /// <param name="parser">解析器实例</param>
        public void AddRPCParser(string key, IRPCParser parser)
        {
            this.RPCParsers.Add(key, parser);
            parser.SetRPCService(this);
            parser.SetExecuteMethod(PreviewExecuteMethod);
            parser.SetMethodMap(this.MethodMap);
        }

        /// <summary>
        /// 添加RPC解析器
        /// </summary>
        /// <param name="key">名称</param>
        /// <param name="parser">解析器实例</param>
        /// <param name="applyServer">是否应用已注册服务</param>
        public void AddRPCParser(string key, IRPCParser parser, bool applyServer)
        {
            this.RPCParsers.Add(key, parser);
            parser.SetRPCService(this);
            parser.SetExecuteMethod(PreviewExecuteMethod);
            parser.SetMethodMap(this.MethodMap);

            if (applyServer)
            {
                Dictionary<ServerProvider, List<MethodInstance>> pairs = new Dictionary<ServerProvider, List<MethodInstance>>();

                MethodInstance[] instances = this.MethodMap.GetAllMethodInstances();

                foreach (var item in instances)
                {
                    if (!pairs.ContainsKey(item.Provider))
                    {
                        pairs.Add(item.Provider, new List<MethodInstance>());
                    }

                    pairs[item.Provider].Add(item);
                }
                foreach (var item in pairs.Keys)
                {
                    parser.OnRegisterServer(item, pairs[item].ToArray());
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            foreach (var item in this.RPCParsers)
            {
                item.Dispose();
            }
            this.RPCParsers = null;
        }

        /// <summary>
        /// 注册所有服务
        /// </summary>
        /// <returns>返回搜索到的服务数</returns>
        public int RegisterAllServer()
        {
            Type[] types = (AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(s => s.GetTypes()).Where(p => typeof(ServerProvider).IsAssignableFrom(p) && p.IsAbstract == false)).ToArray();

            foreach (Type type in types)
            {
                ServerProvider serverProvider = Activator.CreateInstance(type) as ServerProvider;
                RegisterServer(serverProvider);
            }
            return types.Length;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>返回T实例</returns>
        public ServerProvider RegisterServer<T>() where T : ServerProvider
        {
            ServerProvider serverProvider = (ServerProvider)Activator.CreateInstance(typeof(T));
            this.RegisterServer(serverProvider);
            return serverProvider;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        public ServerProvider RegisterServer(Type providerType)
        {
            if (!typeof(ServerProvider).IsAssignableFrom(providerType))
            {
                throw new RRQMRPCException("类型不相符");
            }
            ServerProvider serverProvider = (ServerProvider)Activator.CreateInstance(providerType);
            this.RegisterServer(serverProvider);
            return serverProvider;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="serverProvider"></param>
        public void RegisterServer(ServerProvider serverProvider)
        {
            serverProvider.RPCService = this;
            this.ServerProviders.Add(serverProvider);

            if (this.RPCParsers.Count == 0)
            {
                throw new RRQMRPCException("请至少添加一种RPC解析器");
            }
            MethodInstance[] methodInstances = Tools.GetMethodInstances(serverProvider, true);

            foreach (var item in methodInstances)
            {
                this.MethodMap.Add(item);
            }
            foreach (var parser in this.RPCParsers)
            {
                parser.OnRegisterServer(serverProvider, methodInstances);
            }
        }

        /// <summary>
        /// 移除RPC解析器
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parser"></param>
        public void RemoveRPCParser(string key, out IRPCParser parser)
        {
            if (!this.RPCParsers.TryRemove(key, out parser))
            {
                throw new RRQMException("没有找到该解析器");
            }
        }
       
        /// <summary>
        /// 设置服务方法可用性
        /// </summary>
        /// <param name="methodToken">方法名</param>
        /// <param name="enable">可用性</param>
        /// <exception cref="RRQMRPCException"></exception>
        public void SetMethodEnable(int methodToken, bool enable)
        {
            if (this.MethodMap.TryGet(methodToken, out MethodInstance methodInstance))
            {
                methodInstance.IsEnable = enable;
            }
            else
            {
                throw new RRQMRPCException("未找到该方法");
            }
        }

        /// <summary>
        /// 获取解析器
        /// </summary>
        /// <param name="parserKey"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        public bool TryGetRPCParser(string parserKey, out IRPCParser parser)
        {
            return this.RPCParsers.TryGetRPCParser(parserKey, out parser);
        }
       
        /// <summary>
        /// 移除注册服务
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public int UnregisterServer(ServerProvider provider)
        {
            return this.UnregisterServer(provider.GetType());
        }

        /// <summary>
        /// 移除注册服务
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        public int UnregisterServer(Type providerType)
        {
            if (!typeof(ServerProvider).IsAssignableFrom(providerType))
            {
                throw new RRQMRPCException("类型不相符");
            }
            this.ServerProviders.Remove(providerType);
            if (this.MethodMap.RemoveServer(providerType, out ServerProvider serverProvider, out MethodInstance[] instances))
            {
                foreach (var parser in this.RPCParsers)
                {
                    parser.OnUnregisterServer(serverProvider, instances);
                }

                return instances.Length;
            }
            return 0;
        }

        /// <summary>
        /// 移除注册服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int UnregisterServer<T>() where T : ServerProvider
        {
            return this.UnregisterServer(typeof(T));
        }

        private void ExecuteMethod(bool isAsync, IRPCParser parser, MethodInvoker methodInvoker, MethodInstance methodInstance)
        {
            if (methodInvoker.Status == InvokeStatus.Ready && methodInstance != null)
            {
                try
                {
                    methodInstance.Provider.RPC(1, parser, methodInvoker, methodInstance);
                    if (isAsync)
                    {
                        dynamic task = methodInstance.Method.Invoke(methodInstance.Provider, methodInvoker.Parameters);
                        task.Wait();
                        if (methodInstance.ReturnType!=null)
                        {
                            methodInvoker.ReturnParameter = task.Result;
                        }
                    }
                    else
                    {
                        methodInvoker.ReturnParameter = methodInstance.Method.Invoke(methodInstance.Provider, methodInvoker.Parameters);
                    }
                    methodInstance.Provider.RPC(3, parser, methodInvoker, methodInstance);
                    methodInvoker.Status = InvokeStatus.Success;
                }
                catch (RRQMAbandonRPCException e)
                {
                    methodInvoker.Status = InvokeStatus.Abort;
                    methodInvoker.StatusMessage = "函数被阻止执行，信息：" + e.Message;
                }
                catch (TargetInvocationException e)
                {
                    methodInvoker.Status = InvokeStatus.InvocationException;
                    if (e.InnerException != null)
                    {
                        methodInvoker.StatusMessage = "函数内部发生异常，信息：" + e.InnerException.Message;
                    }
                    else
                    {
                        methodInvoker.StatusMessage = "函数内部发生异常，信息：未知";
                    }
                    methodInstance.Provider.RPC(1, parser, methodInvoker, methodInstance);
                }
                catch (Exception e)
                {
                    methodInvoker.Status = InvokeStatus.Exception;
                    methodInvoker.StatusMessage = e.Message;
                    methodInstance.Provider.RPC(1, parser, methodInvoker, methodInstance);
                }
            }

            parser.OnEndInvoke(methodInvoker, methodInstance);
        }

        private void PreviewExecuteMethod(IRPCParser parser, MethodInvoker methodInvoker, MethodInstance methodInstance)
        {
            if (methodInstance != null && methodInstance.Async)
            {
                Task.Run(() =>
                {
                    ExecuteMethod(true, parser, methodInvoker, methodInstance);
                });
            }
            else
            {
                ExecuteMethod(false, parser, methodInvoker, methodInstance);
            }
        }
    }
}