<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SubGroupInfo.ascx.cs" Inherits="com.reallifeministries.SubGroupInfo" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:panel ID="pnlContent" runat="server">
            <Rock:Grid runat="server" ID="gSubGroups" OnGridRebind="gSubGroups_GridRebind">
                <Columns>
                    
                </Columns>
            </Rock:Grid>
        </asp:panel>
    </ContentTemplate>
</asp:UpdatePanel>