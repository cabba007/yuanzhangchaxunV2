using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace yuanzhangchaxunV2
{
    public partial class up : System.Web.UI.Page
    {
        protected void Button1_Click(object sender, EventArgs e)
        {
            if (Request.Files["file"].ContentLength > 0)
            {
                Request.Files["file"].SaveAs("\\\\10.10.33.173\\8288\\" + System.IO.Path.GetFileName(Request.Files["file"].FileName));
                Label1.Text = "上传成功！";
            }
        } 

        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}