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
using RRQMCore.ByteManager;
using RRQMCore.Log;
using RRQMCore.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RRQMSocket.RPC.RRQMRPC
{
    /// <summary>
    /// UDP RPC解释器
    /// </summary>
    public class UdpRPCParser : UdpSession, IRPCParser, IRRQMRPCParser
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public UdpRPCParser()
        {
            this.methodStore = new MethodStore();
            this.proxyInfo = new RPCProxyInfo();
        }

#pragma warning disable
        public MethodMap MethodMap { get; private set; }

        public RPCService RPCService { get; private set; }

        public Action<IRPCParser, MethodInvoker, MethodInstance> RRQMExecuteMethod { get; private set; }

        public CellCode[] Codes { get => this.proxyInfo == null ? null : this.proxyInfo.Codes.ToArray(); }

        public string NameSpace { get; private set; }

        public RPCProxyInfo ProxyInfo { get => proxyInfo; }

        public string ProxyToken { get; private set; }

        public Version RPCVersion { get; private set; }

        public SerializeConverter SerializeConverter { get; private set; }

        private MethodStore methodStore;
        private RPCProxyInfo proxyInfo;
        public MethodStore MethodStore => this.methodStore;

        public void SetExecuteMethod(Action<IRPCParser, MethodInvoker, MethodInstance> executeMethod)
        {
            this.RRQMExecuteMethod = executeMethod;
        }

        public void SetMethodMap(MethodMap methodMap)
        {
            this.MethodMap = methodMap;
        }

        public void SetRPCService(RPCService service)
        {
            this.RPCService = service;
        }

        public virtual RPCProxyInfo GetProxyInfo(string proxyToken, object caller)
        {
            RPCProxyInfo proxyInfo = new RPCProxyInfo();
            if (this.ProxyToken == proxyToken)
            {
                proxyInfo.AssemblyData = this.ProxyInfo.AssemblyData;
                proxyInfo.AssemblyName = this.ProxyInfo.AssemblyName;
                proxyInfo.Codes = this.ProxyInfo.Codes;
                proxyInfo.Version = this.ProxyInfo.Version;
                proxyInfo.Status = 1;
            }
            else
            {
                proxyInfo.Status = 2;
                proxyInfo.Message = "令箭不正确";
            }

            return proxyInfo;
        }

        public virtual void ExecuteContext(RPCContext context, object caller)
        {
            MethodInvoker methodInvoker = new MethodInvoker();
            methodInvoker.Caller = caller;
            methodInvoker.Flag = context;
            if (this.MethodMap.TryGet(context.MethodToken, out MethodInstance methodInstance))
            {
                try
                {
                    if (methodInstance.IsEnable)
                    {
                        object[] ps = new object[methodInstance.ParameterTypes.Length];
                        for (int i = 0; i < methodInstance.ParameterTypes.Length; i++)
                        {
                            ps[i] = this.SerializeConverter.DeserializeParameter(context.ParametersBytes[i], methodInstance.ParameterTypes[i]);
                        }
                        methodInvoker.Parameters = ps;
                    }
                    else
                    {
                        methodInvoker.Status = InvokeStatus.UnEnable;
                    }
                }
                catch (Exception ex)
                {
                    methodInvoker.Status = InvokeStatus.Exception;
                    methodInvoker.StatusMessage = ex.Message;
                }

                this.RRQMExecuteMethod.Invoke(this, methodInvoker, methodInstance);
            }
            else
            {
                methodInvoker.Status = InvokeStatus.UnFound;
                this.RRQMExecuteMethod.Invoke(this, methodInvoker, null);
            }
        }

        protected override void LoadConfig(ServiceConfig ServiceConfig)
        {
            base.LoadConfig(ServiceConfig);
            this.SerializeConverter = (SerializeConverter)ServiceConfig.GetValue(UdpRPCParserConfig.SerializeConverterProperty);
            this.ProxyToken = (string)ServiceConfig.GetValue(UdpRPCParserConfig.ProxyTokenProperty);
            this.NameSpace = (string)ServiceConfig.GetValue(UdpRPCParserConfig.NameSpaceProperty);
            this.RPCVersion = (Version)ServiceConfig.GetValue(UdpRPCParserConfig.RPCVersionProperty);
        }

        public virtual List<MethodItem> GetRegisteredMethodItems(string proxyToken, object caller)
        {
            return this.methodStore.GetAllMethodItem();
        }

        public void OnRegisterServer(ServerProvider provider, MethodInstance[] methodInstances)
        {
            Tools.GetRPCMethod(methodInstances, this.NameSpace, ref this.methodStore, this.RPCVersion, ref this.proxyInfo);
        }

        public void OnUnregisterServer(ServerProvider provider, MethodInstance[] methodInstances)
        {
            foreach (var item in methodInstances)
            {
                this.methodStore.RemoveMethodItem(item.MethodToken);
            }

            CellCode cellCode = null;
            foreach (var item in this.proxyInfo.Codes)
            {
                if (item.Name == provider.GetType().Name)
                {
                    cellCode = item;
                    break;
                }
            }
            if (cellCode != null)
            {
                this.proxyInfo.Codes.Remove(cellCode);
            }

        }

        public void OnEndInvoke(MethodInvoker methodInvoker, MethodInstance methodInstance)
        {
            RPCContext context = (RPCContext)methodInvoker.Flag;
            if (context.Feedback != 2)
            {
                return;
            }
            ByteBlock byteBlock = this.BytePool.GetByteBlock(this.BufferLength);
            try
            {
                switch (methodInvoker.Status)
                {
                    case InvokeStatus.Ready:
                        {
                            break;
                        }

                    case InvokeStatus.UnFound:
                        {
                            context.Status = 2;
                            break;
                        }
                    case InvokeStatus.Success:
                        {
                            if (methodInstance.MethodToken > 50000000)
                            {
                                context.ReturnParameterBytes = this.SerializeConverter.SerializeParameter(methodInvoker.ReturnParameter);
                            }
                            else
                            {
                                context.ReturnParameterBytes = null;
                            }

                            if (methodInstance.IsByRef)
                            {
                                context.ParametersBytes = new List<byte[]>();
                                foreach (var item in methodInvoker.Parameters)
                                {
                                    context.ParametersBytes.Add(this.SerializeConverter.SerializeParameter(item));
                                }
                            }
                            else
                            {
                                context.ParametersBytes = null;
                            }

                            context.Status = 1;
                            break;
                        }
                    case InvokeStatus.Abort:
                        {
                            context.Status = 4;
                            context.Message = methodInvoker.StatusMessage;
                            break;
                        }
                    case InvokeStatus.UnEnable:
                        {
                            context.Status = 3;
                            break;
                        }
                    case InvokeStatus.InvocationException:
                        {
                            context.Status = 5;
                            context.Message = methodInvoker.StatusMessage;
                            break;
                        }
                    case InvokeStatus.Exception:
                        {
                            context.Status = 6;
                            context.Message = methodInvoker.StatusMessage;
                            break;
                        }
                    default:
                        break;
                }

                context.Serialize(byteBlock);
                this.UDPSend(101, (EndPoint)methodInvoker.Caller, byteBlock.Buffer, 0, byteBlock.Len);
            }
            catch (Exception ex)
            {
                Logger.Debug(LogType.Error, this, ex.Message);
            }
            finally
            {
                byteBlock.Dispose();
            }
        }

#pragma warning restore

        private void UDPSend(short procotol, EndPoint endPoint, byte[] buffer, int offset, int length)
        {
            ByteBlock byteBlock = this.BytePool.GetByteBlock(length + 2);
            try
            {
                byteBlock.Write(BitConverter.GetBytes(procotol));
                byteBlock.Write(buffer, offset, length);
                this.SendTo(byteBlock.Buffer, 0, byteBlock.Len, endPoint);
            }
            finally
            {
                byteBlock.Dispose();
            }
        }

        private void UDPSend(short procotol, EndPoint endPoint, byte[] buffer)
        {
            this.UDPSend(procotol, endPoint, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 密封处理
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="byteBlock"></param>
        protected sealed override void HandleReceivedData(EndPoint remoteEndPoint, ByteBlock byteBlock)
        {
            byte[] buffer = byteBlock.Buffer;
            int r = byteBlock.Len;
            short procotol = BitConverter.ToInt16(buffer, 0);

            switch (procotol)
            {
                case 100:/*100，请求RPC文件*/
                    {
                        try
                        {
                            string proxyToken = Encoding.UTF8.GetString(buffer, 2, r - 2);
                            this.UDPSend(100, remoteEndPoint, SerializeConvert.RRQMBinarySerialize(this.GetProxyInfo(proxyToken, remoteEndPoint), true));
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(LogType.Error, this, $"UDP错误代码: 100, 错误详情:{e.Message}");
                        }
                        break;
                    }

                case 101:/*函数式调用*/
                    {
                        try
                        {
                            RPCContext content = RPCContext.Deserialize(buffer, 2);
                            if (content.Feedback == 1)
                            {
                                List<byte[]> ps = content.ParametersBytes;

                                ByteBlock returnByteBlock = this.BytePool.GetByteBlock(this.bufferLength);
                                try
                                {
                                    content.ParametersBytes = null;
                                    content.Status = 1;
                                    content.Serialize(returnByteBlock);
                                    this.UDPSend(101, remoteEndPoint, returnByteBlock.Buffer, 0, (int)returnByteBlock.Length);
                                }
                                finally
                                {
                                    content.ParametersBytes = ps;
                                    returnByteBlock.Dispose();
                                }
                            }
                            this.ExecuteContext(content, remoteEndPoint);
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(LogType.Error, this, $"错误代码: 101, 错误详情:{e.Message}");
                        }
                        break;
                    }
                case 102:/*连接初始化*/
                    {
                        try
                        {
                            string proxyToken = Encoding.UTF8.GetString(buffer, 2, r - 2);
                            UDPSend(102, remoteEndPoint, SerializeConvert.RRQMBinarySerialize(this.GetRegisteredMethodItems(proxyToken, remoteEndPoint), true));
                        }
                        catch (Exception e)
                        {
                            Logger.Debug(LogType.Error, this, $"错误代码: 102, 错误详情:{e.Message}");
                        }
                        break;
                    }
            }
        }

#if NET45_OR_GREATER
        /// <summary>
        /// 编译代理
        /// </summary>
        /// <param name="targetDic">存放目标文件夹</param>
        public void CompilerProxy(string targetDic = "")
        {
            string assemblyInfo = CodeMap.GetAssemblyInfo(this.proxyInfo.AssemblyName, this.proxyInfo.Version);
            List<string> codesString = new List<string>();
            codesString.Add(assemblyInfo);
            foreach (var item in this.proxyInfo.Codes)
            {
                codesString.Add(item.Code);
            }
            RPCCompiler.CompileCode(Path.Combine(targetDic, this.proxyInfo.AssemblyName), codesString.ToArray());

        }
#endif

       
       
    }
}