


                       AXIS Media Control 
                  Software Development Kit (SDK)


The AXIS Media Control Software Development Kit (SDK) contains 
the AXIS Media Control (AMC) ActiveX and related software, 
documentation and samples to aid in the development of any 
applications that require the component's features.

__________________________________________________________________


INSTALLATION
============

- AXIS Media Control SDK can be installed on any Windows platform, 
  but only Windows XP or higher is supported.

- Client PC requirements for MPEG-4 and H264 (one instance):

    CPU
      • Pentium 4 or higher

    Memory
      • 128 MB RAM

    Graphic card
      • AGP card with Direct Draw support and 16 Mb video memory
    	(512 Mb recommended for high resolutions/color depths)

    Software
      • Internet Explorer 5.5 or higher
      • DirectX 9.0 or higher
      • Windows Media Format 9 Series Runtime, or higher, is 
	required for supporting MPEG recording in AXIS Media 
        Control.

- Administrator rights on the target platform are required
  when installing the AXIS Media Control SDK.
  
- The AXIS Media Control SDK does not include an MPEG-4 video 
  decoder or an AAC audio decoder. 


UNINSTALLATION
==============

To uninstall, use "Add/Remove Programs" from the Control Panel.


FILE LAYOUT
===========

The following is a brief description of the directories found
after installation of the AXIS Media Control SDK.

\Bin
    executable files required for full AMC functionality.
    Includes the AMC ActiveX (AxisMediaControl.dll) and a
    series of components and DirectShow filters required 
    to support Axis media streams and files.
    
\Doc
    contains reference documentation for the AMC ActiveX.
    This documentation must be viewed with Windows HTMLHelp.  

    Note:  The HTMLHelp viewer requires Internet Explorer 5.0
    or higher. IE can be found at
    http://www.microsoft.com/windows/ie/default.htm.

\Redist
    contains redistribution packages to use in your
    application's setup process and on web pages.
  
\Samples
    contains sample code and sample binaries for different
    tools and development environments.
