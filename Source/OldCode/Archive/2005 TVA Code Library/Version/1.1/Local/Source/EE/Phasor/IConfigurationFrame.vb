'*******************************************************************************************************
'  IConfigurationFrame.vb - Configuration frame interface
'  Copyright � 2005 - TVA, all rights reserved - Gbtc
'
'  Build Environment: VB.NET, Visual Studio 2003
'  Primary Developer: James R Carroll, System Analyst [TVA]
'      Office: COO - TRNS/PWR ELEC SYS O, CHATTANOOGA, TN - MR 2W-C
'       Phone: 423/751-2827
'       Email: jrcarrol@tva.gov
'
'  Code Modification History:
'  -----------------------------------------------------------------------------------------------------
'  01/14/2005 - James R Carroll
'       Initial version of source generated
'
'*******************************************************************************************************

Namespace EE.Phasor

    ' This interface represents the protocol independent representation of any configuration frame.
    Public Interface IConfigurationFrame

        Inherits IChannelFrame

        Shadows ReadOnly Property Cells() As ConfigurationCellCollection

        Property SampleRate() As Int16

        ' TODO: Move nominal frequency down into config cell definition
        Property NominalFrequency() As LineFrequency

    End Interface

End Namespace