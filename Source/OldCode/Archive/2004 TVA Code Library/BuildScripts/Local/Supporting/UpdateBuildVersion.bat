@ECHO OFF

ECHO �
ECHO ��������������������������������������������ͻ
ECHO �   Updating Build Number in Source Files... �
ECHO ��������������������������������������������ͼ
ECHO �

COPY /Y %DEPLOYPATH%Builds\Build.ver %SOURCE%Build.ver

%UTILITY%IncrementBuildNumber %SOURCE%Build.ver
CALL %SUPPORT%GetBuildVersion.bat >NUL
%UTILITY%ReplaceInFiles -r -i -x %SOURCE%AssemblyInfo.* "AssemblyVersion(Attribute)?\(&quot;((\*|\d+)\.)+(\*|\d+)&quot;\)" "AssemblyVersion(&quot;%BUILDVER%&quot;)"
%UTILITY%ReplaceInFiles -r -i -x %SOURCE%Config\VariableEvaluator.js "AssemblyVersion(Attribute)?\(&quot;((\*|\d+)\.)+(\*|\d+)&quot;\)" "AssemblyVersion(&quot;%BUILDVER%&quot;)"
%UTILITY%ReplaceInFiles -r -i -x %SOURCE%*.rc "(\d+\.)+\d+" "%BUILDVER%"
%UTILITY%ReplaceInFiles -r -i -x %SOURCE%*.rc "(\d+,)+\d+" "%RCBUILDVER%"

COPY /Y %SOURCE%Build.ver %DEPLOYPATH%Builds\Build.ver