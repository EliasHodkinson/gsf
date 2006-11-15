' 09-26-06

Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Drawing
Imports System.ComponentModel
Imports Tva.Assembly
Imports Tva.IO.Common
Imports Tva.IO.Compression
Imports Tva.Security.Cryptography.Common

Namespace Application

    <ToolboxBitmap(GetType(WebSecurityProvider))> _
    Public Class WebSecurityProvider

#Region " Member Declaration "

        Private WithEvents m_parent As System.Web.UI.Page

        ''' <summary>
        ''' Key used for storing the username.
        ''' </summary>
        Private Const UNKey As String = "u"

        ''' <summary>
        ''' Key used for storing the password.
        ''' </summary>
        Private Const PWKey As String = "p"

        ''' <summary>
        ''' Key used for storing whether or not the supported web file have been extracted.
        ''' </summary>
        Private Const WEKey As String = "we"

        ''' <summary>
        ''' Key used for storing the user data.
        ''' </summary>
        Private Const UDKey As String = "ud"

        ''' <summary>
        ''' Name of the cookie that will contain the current user's credentials.
        ''' </summary>
        ''' <remarks>
        ''' This cookie is used for "single-signon" purposes.
        ''' </remarks>
        Private Const CCName As String = "Credentials"

#End Region

#Region " Public Code "

        <Category("Configuration")> _
        Public Property Parent() As System.Web.UI.Page
            Get
                Return m_parent
            End Get
            Set(ByVal value As System.Web.UI.Page)
                If value IsNot Nothing Then
                    m_parent = value
                Else
                    Throw New ArgumentException("Parent cannot be null.")
                End If
            End Set
        End Property

        Public Overrides Sub LogoutUser()

            If MyBase.User IsNot Nothing AndAlso m_parent IsNot Nothing Then
                ' Delete the session cookie used for "single-signon" purposes.
                Dim credentialCookie As New System.Web.HttpCookie(CCName)
                credentialCookie.Expires = System.DateTime.Now.AddDays(-1)
                m_parent.Response.Cookies.Add(credentialCookie)

                ' Remove the username and password from session variables.
                If m_parent.Session(UNKey) IsNot Nothing Then m_parent.Session.Remove(UNKey)
                If m_parent.Session(PWKey) IsNot Nothing Then m_parent.Session.Remove(PWKey)
                If m_parent.Session(UDKey) IsNot Nothing Then m_parent.Session.Remove(UDKey)

                m_parent.Response.Redirect(GetCleanUrl())
            End If

        End Sub

#End Region

#Region " Protected Code "

        Protected Overrides Sub CacheUserData()

            If m_parent IsNot Nothing AndAlso m_parent.Session(UDKey) Is Nothing Then
                ' Cache the current user's data.
                m_parent.Session.Add(UDKey, MyBase.User)
            End If

        End Sub

        Protected Overrides Sub RetrieveUserData()

            If m_parent IsNot Nothing AndAlso m_parent.Session(UDKey) IsNot Nothing Then
                ' Retrieve previously cached user data.
                UpdateUserData(TryCast(m_parent.Session(UDKey), User))
            End If

        End Sub

        Protected Overrides Function GetUsername() As String

            If m_parent IsNot Nothing Then
                Dim username As String = ""

                If m_parent.Request(UNKey) IsNot Nothing Then
                    ' We'll save the username present in the query string to session and cookie for later use.
                    m_parent.Session.Add(UNKey, m_parent.Request(UNKey).ToString())
                    m_parent.Response.Cookies(CCName)(UNKey) = m_parent.Request(UNKey).ToString()
                    username = Decrypt(m_parent.Request(UNKey).ToString(), Cryptography.EncryptLevel.Level4)
                Else
                    If m_parent.Session(UNKey) IsNot Nothing Then
                        ' Retrieve previouslt saved username from session.
                        username = Decrypt(m_parent.Session(UNKey).ToString(), Cryptography.EncryptLevel.Level4)
                    ElseIf m_parent.Request.Cookies(CCName) IsNot Nothing Then
                        ' Retrieve previouslt saved username from cookie.
                        username = Decrypt(m_parent.Request.Cookies(CCName)(UNKey).ToString(), Cryptography.EncryptLevel.Level4)
                    End If
                End If

                Return username
            Else
                Throw New InvalidOperationException("Parent must be set in order to retrieve the username.")
            End If

        End Function

        Protected Overrides Function GetPassword() As String

            If m_parent IsNot Nothing Then
                Dim password As String = ""

                If m_parent.Request(PWKey) IsNot Nothing Then
                    ' We'll save the password present in the query string to session and cookie for later use.
                    m_parent.Session.Add(PWKey, m_parent.Request(PWKey).ToString())
                    m_parent.Response.Cookies(CCName)(PWKey) = m_parent.Request(PWKey).ToString()
                    password = Decrypt(m_parent.Request(PWKey).ToString(), Cryptography.EncryptLevel.Level4)
                Else
                    If m_parent.Session(UNKey) IsNot Nothing Then
                        ' Retrieve previouslt saved username from session.
                        password = Decrypt(m_parent.Session(PWKey).ToString(), Cryptography.EncryptLevel.Level4)
                    ElseIf m_parent.Request.Cookies(CCName) IsNot Nothing Then
                        ' Retrieve previouslt saved username from cookie.
                        password = Decrypt(m_parent.Request.Cookies(CCName)(PWKey).ToString(), Cryptography.EncryptLevel.Level4)
                    End If
                End If

                Return password
            Else
                Throw New InvalidOperationException("Parent must be set in order to retrieve the password.")
            End If

        End Function

#End Region

#Region " Private Code "

        Private Sub ExtractWebFiles()

            If m_parent.Application(WEKey) Is Nothing Then
                ' Extract the embedded web files to the the web site's bin directory.
                Try
                    Dim webFiles As ZipFile = Nothing
                    Dim zipFilePath As String = m_parent.Server.MapPath("~/")
                    Dim zipFileName As String = zipFilePath & "WebFiles.dat"
                    File.WriteAllBytes(zipFileName, ReadStream(CallingAssembly.GetEmbeddedResource("Application.WebFiles.dat")))
                    webFiles = ZipFile.Open(zipFileName)
                    webFiles.Extract("*.*", zipFilePath, UpdateOption.ZipFileIsNewer, True)
                    webFiles.Close()
                    File.Delete(zipFileName)
                    m_parent.Application.Add(WEKey, True)
                Catch ex As Exception
                    ' We most likely encountered some sort of an access violation exception.
                    Throw New AccessViolationException("Failed to extract the required web files.", ex)
                End Try
            End If

        End Sub

        Private Function GetCleanUrl() As String

            With New StringBuilder()
                ' Remove the username and password from querystring if present.
                .Append(m_parent.Request.Url.AbsolutePath)
                .Append("?")
                For Each parameter As String In m_parent.Request.Url.Query.TrimStart("?"c).Split("&"c)
                    Dim key As String = parameter.Split("="c)(0)
                    If Not (key = UNKey OrElse key = PWKey) Then
                        .Append(parameter)
                    End If
                Next

                Return .ToString()
            End With

        End Function

        Private Function GetLocalWebSiteUrl() As String

            With New StringBuilder()
                .Append(m_parent.Request.Url.Scheme)
                .Append(System.Uri.SchemeDelimiter)
                .Append(m_parent.Request.Url.Host)
                .Append(m_parent.Request.ApplicationPath)
                .Append("/")

                Return .ToString()
            End With

        End Function

        Private Function GetRemoteWebSiteUrl() As String

            With New StringBuilder()
                .Append("http://")
                Select Case MyBase.Server
                    Case SecurityServer.Development
                        .Append("chadesoweb.cha.tva.gov")
                    Case SecurityServer.Acceptance
                        .Append("chaaesoweb.cha.tva.gov")
                    Case SecurityServer.Production
                        .Append("troweb.cha.tva.gov")
                End Select
                .Append("/troapplicationsecurity/")

                Return .ToString()
            End With

        End Function

        Private Sub WebSecurityProvider_Successful(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LoginSuccessful

            If m_parent IsNot Nothing Then
                If m_parent.Request(UNKey) IsNot Nothing OrElse m_parent.Request(PWKey) IsNot Nothing Then
                    ' Upon successful login, we'll remove the username and password from the querystring if present.
                    m_parent.Response.Redirect(GetCleanUrl())
                End If
            End If

        End Sub

        Private Sub WebSecurityProvider_Unsuccessful(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LoginUnsuccessful

            If m_parent IsNot Nothing Then
                ExtractWebFiles()   ' Make sure that the required web file exist in the application bin directory.

                With New StringBuilder()
                    Try
                        Dim getRequest As WebRequest = WebRequest.Create(GetLocalWebSiteUrl() & "Login.aspx")
                        getRequest.Credentials = CredentialCache.DefaultCredentials

                        If getRequest.GetResponse() Is Nothing Then
                            ' We'll redirect to the "Login Page" on predefined remote web site if we're unable
                            ' to request the page locally. This will be case when the developer is debugging the
                            ' the web site from Visual Studio, since the page we're trying to access is embedded.
                            .Append(GetRemoteWebSiteUrl())
                        End If
                    Catch ex As Exception
                        .Append(GetRemoteWebSiteUrl())
                    End Try
                    .Append("Login.aspx?r=")    ' Return Url
                    .Append(m_parent.Server.UrlEncode(m_parent.Request.Url.AbsoluteUri))
                    .Append("&a=")              ' Application Name
                    .Append(m_parent.Server.UrlEncode(Encrypt(MyBase.ApplicationName, Security.Cryptography.EncryptLevel.Level4)))
                    .Append("&c=")              ' Connection String
                    .Append(m_parent.Server.UrlEncode(Encrypt(MyBase.ConnectionString, Security.Cryptography.EncryptLevel.Level4)))

                    m_parent.Response.Redirect(.ToString())
                End With
            Else
                Throw New InvalidOperationException("Parent must be set in order to login the user.")
            End If

        End Sub

        Private Sub m_parent_PreLoad(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_parent.PreLoad

            If MyBase.User Is Nothing Then
                ' EndInit() method of the ISupportInitialize interface was not called which in-turn calls the
                ' LoginUser() method, so we'll call LoginUser() over here implicitly before the web page loads.
                LoginUser()
            End If

        End Sub

        Private Sub m_parent_Unload(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_parent.Unload

            Dispose()

        End Sub

#End Region

    End Class

End Namespace