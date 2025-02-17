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
using RRQMCore.Dependency;

namespace RRQMSocket.FileTransfer
{
    /// <summary>
    /// 文件客户端配置
    /// </summary>
    public class FileClientConfig : TokenClientConfig
    {
        /// <summary>
        /// 默认接收文件的存放目录
        /// </summary>
        public string ReceiveDirectory
        {
            get { return (string)GetValue(ReceiveDirectoryProperty); }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                SetValue(ReceiveDirectoryProperty, value);
            }
        }

        /// <summary>
        /// 默认接收文件的存放目录, 所需类型<see cref="string"/>
        /// </summary>
        public static readonly DependencyProperty ReceiveDirectoryProperty =
            DependencyProperty.Register("ReceiveDirectory", typeof(string), typeof(FileClientConfig), string.Empty);

        /// <summary>
        /// 单次请求超时时间 min=5,max=60 单位：秒
        /// </summary>
        public int Timeout
        {
            get { return (int)GetValue(TimeoutProperty); }
            set
            {
                value = value < 5 ? 5 : (value > 60 ? 60 : value);
                SetValue(TimeoutProperty, value);
            }
        }

        /// <summary>
        /// 单次请求超时时间 min=5,max=60 单位：秒, 所需类型<see cref="int"/>
        /// </summary>
        public static readonly DependencyProperty TimeoutProperty =
            DependencyProperty.Register("Timeout", typeof(int), typeof(FileClientConfig), 5);
    }
}