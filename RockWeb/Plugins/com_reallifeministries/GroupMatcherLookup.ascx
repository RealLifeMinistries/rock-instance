<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMatcherLookup.ascx.cs" Inherits="com.reallifeministries.GroupMatcherLookup" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlSetup" >
            <asp:ValidationSummary runat="server" ID="ValidationSummary" CssClass="alert alert-danger" />
            <fieldset>
                <legend>Configuration</legend>
                <Rock:RockDropDownList runat="server" ID="pkrGroupType" Label="Group Type" DataTextField="Name" DataValueField="Id" Required="true" />
                <Rock:DaysOfWeekPicker runat="server" ID="pkrDaysOfWeek" Label="Days Of Week" Required="true" />
                <Rock:NumberBox runat="server" ID="tbAcceptableRadius" Label="Acceptable Mile Radius" />
            </fieldset>                      
            <fieldset>
                <legend>Person</legend>           
                <Rock:PersonPicker runat="server" ID="pkrPerson" Label="Person" Required="true" />        
                <Rock:BootstrapButton runat="server" cssClass='btn btn-primary' ID="btnSubmit" Text="Find Matches" OnClick="btnSubmit_Click" />
            </fieldset>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlResults" Visible="false">
            <Rock:Grid runat="server" ID="grdMatches">
                <Columns>
                    <asp:BoundField DataField="Group.Name" HeaderText="Name" />
                    <asp:BoundField DataField="Group.ID" HeaderText="ID" />
                    <asp:BoundField DataField="Distance" HeaderText="Distance" />
                </Columns>
            </Rock:Grid>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>