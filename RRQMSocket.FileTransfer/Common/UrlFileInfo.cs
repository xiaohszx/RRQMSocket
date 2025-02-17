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

using RRQMCore.IO;
using System.IO;

namespace RRQMSocket.FileTransfer
{
    /// <summary>
    /// 文件信息类
    /// </summary>
    public class UrlFileInfo : FileInfo
    {
        /// <summary>
        /// 生成下载请求必要信息
        /// </summary>
        /// <param name="path"></param>
        /// <param name="restart"></param>
        /// <returns></returns>
        public static UrlFileInfo CreatDownload(string path, bool restart = false)
        {
            UrlFileInfo fileInfo = new UrlFileInfo();
            fileInfo.FilePath = path;
            fileInfo.Restart = restart;
            fileInfo.FileName = Path.GetFileName(path);
            fileInfo.TransferType = TransferType.Download;
            return fileInfo;
        }

        /// <summary>
        /// 生成上传请求必要信息
        /// </summary>
        /// <param name="path"></param>
        /// <param name="breakpointResume"></param>
        /// <param name="restart"></param>
        /// <returns></returns>
        public static UrlFileInfo CreatUpload(string path, bool breakpointResume, bool restart = false)
        {
            UrlFileInfo fileInfo = new UrlFileInfo();
            fileInfo.TransferType = TransferType.Upload;
            using (FileStream stream = File.OpenRead(path))
            {
                fileInfo.Restart = restart;
                fileInfo.FilePath = path;
                if (breakpointResume)
                {
                    fileInfo.FileHash = FileControler.GetStreamHash(stream);
                }
                fileInfo.FileLength = stream.Length;
                fileInfo.FileName = Path.GetFileName(path);
            }

            return fileInfo;
        }

        /// <summary>
        /// 重新开始
        /// </summary>
        public bool Restart { get; set; }

        /// <summary>
        /// 超时时间，默认30秒
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// 请求传输类型
        /// </summary>
        public TransferType TransferType { get; set; }

        private string saveFolder = string.Empty;

        /// <summary>
        /// 存放目录
        /// </summary>
        public string SaveFolder
        {
            get { return saveFolder; }
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }
                saveFolder = value;
            }
        }

        /// <summary>
        /// 比较
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public bool Equals(UrlFileInfo fileInfo)
        {
            if (this.FileHash == fileInfo.FileHash)
            {
                return true;
            }
            else if (this.FilePath == fileInfo.FilePath)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}