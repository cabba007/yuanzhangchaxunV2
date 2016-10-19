<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="up.aspx.cs" Inherits="yuanzhangchaxunV2.up" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
</head>
<body>
    <form id="form1" runat="server" enctype="multipart/form-data">
        <input type="file" name="file" />
        <asp:Button ID="Button1" runat="server" Text="上传" OnClick="Button1_Click" />
        <asp:Label ID="Label1" runat="server" Text="" Style="color: Red"></asp:Label>
    </form>
</body>
</html>
