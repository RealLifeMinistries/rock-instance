<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SubGroupInfo.ascx.cs" Inherits="com.reallifeministries.SubGroupInfo" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:panel ID="pnlContent" runat="server">
            
            <Rock:Grid runat="server" ID="gSubGroups" OnGridRebind="gSubGroups_GridRebind">
                <Columns>
                    <Rock:RockBoundField DataField="Group.Name" HeaderText="Name" />
                    <Rock:RockBoundField DataField="ActiveMembers" HeaderText="Active" />
                    <Rock:RockBoundField DataField="InactiveMembers" HeaderText="Inactive" />
                    <Rock:RockBoundField DataField="PendingMembers" HeaderText="Pending" />
                </Columns>
            </Rock:Grid>
        </asp:panel>
    </ContentTemplate>
</asp:UpdatePanel>