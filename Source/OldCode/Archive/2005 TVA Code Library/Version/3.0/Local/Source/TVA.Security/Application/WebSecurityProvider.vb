'*******************************************************************************************************
'  TVA.Security.Application.WebSecurityProvider.vb - Security provider for web applications
'  Copyright � 2006 - TVA, all rights reserved - Gbtc
'
'  Build Environment: VB.NET, Visual Studio 2005
'  Primary Developer: Pinal C. Patel, Operations Data Architecture [TVA]
'      Office: COO - TRNS/PWR ELEC SYS O, CHATTANOOGA, TN - MR 2W-C
'       Phone: 423/751-2250
'       Email: pcpatel@tva.gov
'
'  Code Modification History:
'  -----------------------------------------------------------------------------------------------------
'  09-22-06 - Pinal C. Patel
'       Original version of source code generated.
'  09-22-06 - Pinal C. Patel
'       Added the flexibility of providing an absolute URL in config file for externally 
'       facing web sites.
'  12-28-06 - Pinal C. Patel
'       Modified the DatabaseException event handler to display the actual exception message instead 
'       of the previously displayed message that assumed that database connectivity failed.
'
'*******************************************************************************************************

Imports System.Text
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Web.UI.HtmlControls
Imports System.Drawing
Imports System.ComponentModel
Imports TVA.Security.Cryptography.Common
Imports TVA.Security.Application.Controls

Namespace Application

    <ToolboxBitmap(GetType(WebSecurityProvider)), DisplayName("WebSecurityProvider")> _
    Public Class WebSecurityProvider

#Region " Member Declaration "

        Private m_locked As Boolean

        Private WithEvents m_parent As Page

        ''' <summary>
        ''' ID of the server control used to capture user input.
        ''' </summary>
        Private Const SecurityControlID As String = "SecurityProvider"

#End Region

#Region " Code Scope: Public Code "

        ''' <summary>
        ''' Key used for accessing security data in the session.
        ''' </summary>
        Public Const DataKey As String = "SP.Data"

        ''' <summary>
        ''' Key used for accessing the username in the session.
        ''' </summary>
        Public Const UsernameKey As String = "SP.Username"

        ''' <summary>
        ''' Key used for accessing the password in the session.
        ''' </summary>
        Public Const PasswordKey As String = "SP.Password"

        ''' <summary>
        ''' Name of the cookie that will contain the current user's credentials.
        ''' </summary>
        ''' <remarks>
        ''' This cookie is used for "single-signon" purposes.
        ''' </remarks>
        Public Const CredentialCookie As String = "SP.Credentials"

        <Category("Configuration")> _
        Public Property Parent() As Page
            Get
                Return m_parent
            End Get
            Set(ByVal value As Page)
                m_parent = value
            End Set
        End Property

        Public Overrides Sub LogoutUser()

            If User IsNot Nothing AndAlso m_parent IsNot Nothing Then
                ' Abandon the session so all of the session data is removed upon refresh.
                m_parent.Session.Abandon()

                ' Delete the session cookie for "single-signon" purposes if one is created.
                If m_parent.Request.Cookies(CredentialCookie) IsNot Nothing Then
                    Dim cookie As New System.Web.HttpCookie(CredentialCookie)
                    cookie.Expires = System.DateTime.Now.AddDays(-1)
                    m_parent.Response.Cookies.Add(cookie)
                End If

                m_parent.Response.Redirect(m_parent.Request.Url.AbsoluteUri)    'Refresh.
            End If

        End Sub

#Region " Shared "

        ''' <summary>
        ''' Saves security data to the session.
        ''' </summary>
        ''' <param name="page">Page through which session can be accessed.</param>
        ''' <param name="data">Security data to be saved in the session.</param>
        ''' <returns>True if security data is saved; otherwise False.</returns>
        Public Shared Function SaveToCache(ByVal page As Page, ByVal data As WebSecurityProvider) As Boolean

            If page.Session(DataKey) Is Nothing Then
                ' Before caching the security control in the current user's session, we break-off the reference 
                ' that the security control has to the page so that the page doesn't get cached unnecessarily.
                data.Parent = Nothing
                page.Session(WebSecurityProvider.DataKey) = data

                Return True
            End If

        End Function

        ''' <summary>
        ''' Loads security data from the session.
        ''' </summary>
        ''' <param name="page">Page through which session can be accessed.</param>
        ''' <returns>Security data if it exists in the session; otherwise Nothing.</returns>
        Public Shared Function LoadFromCache(ByVal page As Page) As WebSecurityProvider

            Dim data As WebSecurityProvider = TryCast(page.Session(DataKey), WebSecurityProvider)
            If data IsNot Nothing Then
                ' The security control had been cached previously in the current user's session.
                data.Parent = page
            End If

            Return data

        End Function

#End Region

#End Region

#Region " Code Scope: Protected Code "

        Protected Overrides Sub ShowLoginPrompt()

            ' Lock the page and show the "Login" control.
            LockPage("Login")

        End Sub

        Protected Overrides Sub HandleAccessGranted()

            ' We don't need to do anything special here.

        End Sub

        Protected Overrides Sub HandleAccessDenied()

            ' Lock the page show and show the "Access Denied" message.
            With LockPage(String.Empty)
                .MessageText = "<h5>ACCESS DENIED</h5>You are not authorized to view this page."
            End With

        End Sub

        Protected Overrides Function GetUsername() As String

            If m_parent IsNot Nothing Then
                Dim username As String = ""
                Try
                    If m_parent.Session(UsernameKey) IsNot Nothing Then
                        ' Retrieve previously saved username from session.
                        username = m_parent.Session(UsernameKey).ToString()
                    ElseIf AuthenticationMode <> AuthenticationMode.RSA AndAlso _
                            m_parent.Request.Cookies(CredentialCookie) IsNot Nothing Then
                        ' Retrieve previously saved username from cookie, but not when RSA security is employed.
                        username = m_parent.Request.Cookies(CredentialCookie)(UsernameKey).ToString()
                    End If
                Catch ex As Exception
                    ' If we fail to get the username, we'll return an empty string (this way login will fail).
                End Try

                Return Decrypt(username)
            Else
                Throw New InvalidOperationException("Parent must be set in order to retrieve the username.")
            End If

        End Function

        Protected Overrides Function GetPassword() As String

            If m_parent IsNot Nothing Then
                Dim password As String = ""
                Try
                    If m_parent.Session(UsernameKey) IsNot Nothing Then
                        ' Retrieve previously saved password from session.
                        password = m_parent.Session(PasswordKey).ToString()
                    ElseIf AuthenticationMode <> AuthenticationMode.RSA AndAlso _
                            m_parent.Request.Cookies(CredentialCookie) IsNot Nothing Then
                        ' Retrieve previously saved password from cookie, but not when RSA security is employed.
                        password = m_parent.Request.Cookies(CredentialCookie)(PasswordKey).ToString()
                    End If
                Catch ex As Exception
                    ' If we fail to get the username, we'll return an empty string (this way login will fail).
                End Try

                Return Decrypt(password)
            Else
                Throw New InvalidOperationException("Parent must be set in order to retrieve the password.")
            End If

        End Function

#End Region

#Region " Code Scope: Private Code "

        Private Function LockPage(ByVal activeControl As String) As ControlContainer

            If m_parent IsNot Nothing Then
                ' First we have to find the page's form. We cannot use Page.Form property because it's not set yet.
                Dim form As HtmlForm = Nothing
                If m_parent.Master Is Nothing Then
                    ' Page doesn't have a Master Page.
                    For Each ctrl As Control In m_parent.Controls
                        form = TryCast(ctrl, HtmlForm)
                        If form IsNot Nothing Then Exit For
                    Next
                Else
                    ' Page has a Master Page, so the form resides there.
                    For Each ctrl As Control In m_parent.Master.Controls
                        form = TryCast(ctrl, HtmlForm)
                        If form IsNot Nothing Then Exit For
                    Next
                End If

                ' Next we check if the page has been locked previously. If so, we don't need to repeat the process,
                ' instead we find the security control and return it.
                Dim controlTable As Table = TryCast(form.FindControl(SecurityControlID), Table)
                If controlTable Is Nothing Then
                    ' Page has not been locked yet.
                    Dim control As New ControlContainer(Me, activeControl)

                    ' Add control to the table.
                    controlTable = ControlContainer.NewTable(1, 1)
                    controlTable.ID = SecurityControlID
                    controlTable.HorizontalAlign = HorizontalAlign.Center
                    controlTable.Rows(0).Cells(0).Controls.Add(control)

                    form.Controls.Clear()           ' Clear all controls.
                    form.Controls.Add(controlTable) ' Add the container control.

                    m_locked = True                 ' Indicates that page is in lock-down mode.

                    Return control
                Else
                    ' Page has been locked previously.
                    Return DirectCast(controlTable.Rows(0).Cells(0).Controls(0), ControlContainer)
                End If
            Else
                Throw New InvalidOperationException("Parent property is not set.")
            End If

        End Function

#Region " Event Handlers "

        Private Sub m_parent_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_parent.PreRender

            If m_locked Then
                ' This is the last stop before the page and all of its controls get rendered. It is here that we 
                ' make sure that any dynamic controls that got added after we first locked the page are removed 
                ' from the page.
                Dim controls As New List(Of Control)()
                For Each ctrl As Control In m_parent.Form.Controls
                    ' Get a local copy of all page controls.
                    controls.Add(ctrl)
                Next
                For Each ctrl As Control In controls
                    ' Remove all controls other than the security control.
                    If ctrl.ID <> SecurityControlID Then
                        m_parent.Form.Controls.Remove(ctrl)
                    End If
                Next
            End If

        End Sub

        Private Sub WebSecurityProvider_BeforeLogin(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.BeforeLogin

            If m_parent IsNot Nothing Then
                ' Right before the login process starts, we'll check to see if we have the security data cached in
                ' the session (done when user has access to the application). If so, we'll use the cached data and
                ' save us credential verification and a trip to the database. This is primarily done to improves 
                ' performance in scenarios where a secure web page contains many secure user controls.
                Dim cachedData As WebSecurityProvider = WebSecurityProvider.LoadFromCache(m_parent)
                If cachedData IsNot Nothing Then
                    ' We have cached data.
                    User = cachedData.User  ' Here's where we save credential verification and database trip.
                    Server = cachedData.Server
                    ApplicationName = cachedData.ApplicationName
                    AuthenticationMode = cachedData.AuthenticationMode
                Else
                    ' We don't have cached data, so we'll load settings from config file and continue.
                    LoadSettings()
                End If
            Else
                Throw New InvalidOperationException("Parent property is not set.")
            End If

        End Sub

        Private Sub WebSecurityProvider_AccessGranted(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.AccessGranted

            If m_parent IsNot Nothing Then
                ' User has access to the application, so we'll cache the security data for subsequent uses.
                WebSecurityProvider.SaveToCache(m_parent, Me)
            Else
                Throw New InvalidOperationException("Parent property is not set.")
            End If

        End Sub

        Private Sub WebSecurityProvider_DatabaseException(ByVal sender As Object, ByVal e As GenericEventArgs(Of System.Exception)) Handles Me.DatabaseException

            If m_parent IsNot Nothing Then
                With New StringBuilder()
                    .Append("<html>")
                    .AppendLine()
                    .Append("<head>")
                    .AppendLine()
                    .Append("<Title>Login Aborted</Title>")
                    .AppendLine()
                    .Append("</head>")
                    .AppendLine()
                    .Append("<body>")
                    .AppendLine()
                    .Append("<div style=""font-family: Tahoma; font-size: 8pt; font-weight: bold; text-align: center;"">")
                    .AppendLine()
                    .Append("<span style=""font-size: 22pt; color: red;"">")
                    .Append("Login Process Aborted")
                    .Append("</span><br /><br />")
                    .AppendLine()
                    .AppendFormat("[{0}]", e.Argument.Message)
                    .AppendLine()
                    .Append("</div>")
                    .AppendLine()
                    .Append("</body>")
                    .AppendLine()
                    .Append("</html>")

                    m_parent.Response.Clear()
                    m_parent.Response.Write(.ToString())
                    m_parent.Response.End()
                End With
            Else
                Throw New InvalidOperationException("Parent property is not set.")
            End If

        End Sub

#End Region

#End Region

    End Class

End Namespace