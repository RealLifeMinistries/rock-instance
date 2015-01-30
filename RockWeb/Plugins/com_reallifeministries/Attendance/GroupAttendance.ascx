<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupAttendance.ascx.cs" Inherits="com.reallifeministries.Attendance.GroupAttendance" %>

<Rock:RockUpdatePanel runat="server" ID="rupMain">
    <ContentTemplate>
        <Rock:PanelWidget runat="server" ID="pnlMain" Title="Group Attendance" Expanded="true">
            <Rock:NotificationBox runat="server" ID="nbMessage" Visible="false" />
            <asp:Panel runat="server" ID="pnlForm" Visible="false">
                <Rock:DatePicker runat="server"  id="dpAttendanceDate" Required="true" Label="Attended Date" />
                <Rock:RockCheckBoxList runat="server" ID="cblMembers" Label="Attendees" DataTextField="Person.Name" DataValueField="Id" />
                <Rock:BootstrapButton Text="Record Attendance" runat="server" OnClick="btnRecordAttendance_Click" ID="btnRecordAttendance" CssClass='btn btn-primary' DataLoadingText="Saving..." />
          </asp:Panel>
          <asp:Panel runat="server" ID="pnlResults" Visible="false">
              <h4>Attendees</h4>
              <asp:Repeater runat="server" ID="rptAttendees" OnItemCommand="rptAttendees_ItemCommand">
                  <HeaderTemplate>
                      <ul>
                  </HeaderTemplate>
                  <ItemTemplate>
                      <li><%# Eval("PersonAlias.Person.FullName")%>
                          <Rock:BootstrapButton ID='btnUndo' runat="server" CommandName="Undo" CommandArgument='<%# Eval("Id")%>' 
                              CssClass="btn btn-text">
                              <i class='fa fa-ban'></i> Oops..
                          </Rock:BootstrapButton>
                      </li>
                  </ItemTemplate>
                  <FooterTemplate>
                      </ul>
                  </FooterTemplate>
              </asp:Repeater>
              <Rock:BootstrapButton runat="server" CssClass="btn btn-primary" OnClick="btnReset_Click" ID="btnReset" Text="Record More Attendees" />
          </asp:Panel>
         
        </Rock:PanelWidget> 
    </ContentTemplate>
</Rock:RockUpdatePanel>

<script>
    function addCheckAll() {
        $('#<%= pnlForm.ClientID %>').find('.rock-check-box-list > label')
            .prepend('<input type="checkbox" class="checkAll" /> ');
    }
    jQuery(function ($) {
        addCheckAll();
        
        $(<%= rupMain.ClientID %>).on('change', 'input[type=checkbox].checkAll', function (e) {
            var $checkAll = $(this);
            var $label = $checkAll.closest('label');
            $label.nextAll('.controls').find(':checkbox').each(function () {
                this.checked = $checkAll.prop('checked');
            });
        });
    });
    var prm = Sys.WebForms.PageRequestManager.getInstance();
    prm.add_endRequest(addCheckAll);

</script>
     