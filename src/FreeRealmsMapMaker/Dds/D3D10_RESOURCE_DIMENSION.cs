/*
* 
*  DDSImage.cs - DDS Texture File Reading (Uncompressed, DXT1/2/3/4/5, V8U8) and Writing (Uncompressed Only)
*  
*  By Shendare (Jon D. Jackson)
* 
*  Rebuilt from Microsoft DDS documentation with the help of the DDSImage.cs reading class from
*  Lorenzo Consolaro, under the MIT License.  https://code.google.com/p/kprojects/ 
* 
*  Portions of this code not covered by another author's or entity's copyright are released under
*  the Creative Commons Zero (CC0) public domain license.
*  
*  To the extent possible under law, Shendare (Jon D. Jackson) has waived all copyright and
*  related or neighboring rights to this DDSImage class. This work is published from: The United States. 
*  
*  You may copy, modify, and distribute the work, even for commercial purposes, without asking permission.
* 
*  For more information, read the CC0 summary and full legal text here:
*  
*  https://creativecommons.org/publicdomain/zero/1.0/
* 
*/

namespace FreeRealmsMapMaker.Dds;

internal enum D3D10_RESOURCE_DIMENSION
{
    D3D10_RESOURCE_DIMENSION_UNKNOWN = 0,
    D3D10_RESOURCE_DIMENSION_BUFFER = 1,
    D3D10_RESOURCE_DIMENSION_TEXTURE1D = 2,
    D3D10_RESOURCE_DIMENSION_TEXTURE2D = 3,
    D3D10_RESOURCE_DIMENSION_TEXTURE3D = 4
}
