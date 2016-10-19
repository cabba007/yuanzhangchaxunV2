using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace yuanzhangchaxunV2
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的接口名“IService1”。
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetData?q={q}")]
        ServiceResult GetData(string q);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/UploadFile?fileName={fileName}")]
        string UploadFile(string fileName, System.IO.Stream stream);
    }

}
