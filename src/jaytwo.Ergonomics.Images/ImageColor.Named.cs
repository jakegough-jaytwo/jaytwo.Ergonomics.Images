using System;
using System.Collections.Generic;

namespace jaytwo.Ergonomics.Images;

/// <content>
/// Canonical CSS / web named colors.
/// </content>
public readonly partial record struct ImageColor
{
    private static readonly Dictionary<string, ImageColor> NamedColors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(AliceBlue)] = new(240, 248, 255),
            [nameof(AntiqueWhite)] = new(250, 235, 215),
            [nameof(Aqua)] = new(0, 255, 255),
            [nameof(Aquamarine)] = new(127, 255, 212),
            [nameof(Azure)] = new(240, 255, 255),
            [nameof(Beige)] = new(245, 245, 220),
            [nameof(Bisque)] = new(255, 228, 196),
            [nameof(Black)] = new(0, 0, 0),
            [nameof(BlanchedAlmond)] = new(255, 235, 205),
            [nameof(Blue)] = new(0, 0, 255),
            [nameof(BlueViolet)] = new(138, 43, 226),
            [nameof(Brown)] = new(165, 42, 42),
            [nameof(BurlyWood)] = new(222, 184, 135),
            [nameof(CadetBlue)] = new(95, 158, 160),
            [nameof(Chartreuse)] = new(127, 255, 0),
            [nameof(Chocolate)] = new(210, 105, 30),
            [nameof(Coral)] = new(255, 127, 80),
            [nameof(CornflowerBlue)] = new(100, 149, 237),
            [nameof(Cornsilk)] = new(255, 248, 220),
            [nameof(Crimson)] = new(220, 20, 60),
            [nameof(Cyan)] = new(0, 255, 255),
            [nameof(DarkBlue)] = new(0, 0, 139),
            [nameof(DarkCyan)] = new(0, 139, 139),
            [nameof(DarkGoldenrod)] = new(184, 134, 11),
            [nameof(DarkGray)] = new(169, 169, 169),
            [nameof(DarkGreen)] = new(0, 100, 0),
            [nameof(DarkGrey)] = new(169, 169, 169),
            [nameof(DarkKhaki)] = new(189, 183, 107),
            [nameof(DarkMagenta)] = new(139, 0, 139),
            [nameof(DarkOliveGreen)] = new(85, 107, 47),
            [nameof(DarkOrange)] = new(255, 140, 0),
            [nameof(DarkOrchid)] = new(153, 50, 204),
            [nameof(DarkRed)] = new(139, 0, 0),
            [nameof(DarkSalmon)] = new(233, 150, 122),
            [nameof(DarkSeaGreen)] = new(143, 188, 143),
            [nameof(DarkSlateBlue)] = new(72, 61, 139),
            [nameof(DarkSlateGray)] = new(47, 79, 79),
            [nameof(DarkSlateGrey)] = new(47, 79, 79),
            [nameof(DarkTurquoise)] = new(0, 206, 209),
            [nameof(DarkViolet)] = new(148, 0, 211),
            [nameof(DeepPink)] = new(255, 20, 147),
            [nameof(DeepSkyBlue)] = new(0, 191, 255),
            [nameof(DimGray)] = new(105, 105, 105),
            [nameof(DimGrey)] = new(105, 105, 105),
            [nameof(DodgerBlue)] = new(30, 144, 255),
            [nameof(Firebrick)] = new(178, 34, 34),
            [nameof(FloralWhite)] = new(255, 250, 240),
            [nameof(ForestGreen)] = new(34, 139, 34),
            [nameof(Fuchsia)] = new(255, 0, 255),
            [nameof(Gainsboro)] = new(220, 220, 220),
            [nameof(GhostWhite)] = new(248, 248, 255),
            [nameof(Gold)] = new(255, 215, 0),
            [nameof(Goldenrod)] = new(218, 165, 32),
            [nameof(Gray)] = new(128, 128, 128),
            [nameof(Green)] = new(0, 128, 0),
            [nameof(GreenYellow)] = new(173, 255, 47),
            [nameof(Grey)] = new(128, 128, 128),
            [nameof(Honeydew)] = new(240, 255, 240),
            [nameof(HotPink)] = new(255, 105, 180),
            [nameof(IndianRed)] = new(205, 92, 92),
            [nameof(Indigo)] = new(75, 0, 130),
            [nameof(Ivory)] = new(255, 255, 240),
            [nameof(Khaki)] = new(240, 230, 140),
            [nameof(Lavender)] = new(230, 230, 250),
            [nameof(LavenderBlush)] = new(255, 240, 245),
            [nameof(LawnGreen)] = new(124, 252, 0),
            [nameof(LemonChiffon)] = new(255, 250, 205),
            [nameof(LightBlue)] = new(173, 216, 230),
            [nameof(LightCoral)] = new(240, 128, 128),
            [nameof(LightCyan)] = new(224, 255, 255),
            [nameof(LightGoldenrodYellow)] = new(250, 250, 210),
            [nameof(LightGray)] = new(211, 211, 211),
            [nameof(LightGreen)] = new(144, 238, 144),
            [nameof(LightGrey)] = new(211, 211, 211),
            [nameof(LightPink)] = new(255, 182, 193),
            [nameof(LightSalmon)] = new(255, 160, 122),
            [nameof(LightSeaGreen)] = new(32, 178, 170),
            [nameof(LightSkyBlue)] = new(135, 206, 250),
            [nameof(LightSlateGray)] = new(119, 136, 153),
            [nameof(LightSlateGrey)] = new(119, 136, 153),
            [nameof(LightSteelBlue)] = new(176, 196, 222),
            [nameof(LightYellow)] = new(255, 255, 224),
            [nameof(Lime)] = new(0, 255, 0),
            [nameof(LimeGreen)] = new(50, 205, 50),
            [nameof(Linen)] = new(250, 240, 230),
            [nameof(Magenta)] = new(255, 0, 255),
            [nameof(Maroon)] = new(128, 0, 0),
            [nameof(MediumAquamarine)] = new(102, 205, 170),
            [nameof(MediumBlue)] = new(0, 0, 205),
            [nameof(MediumOrchid)] = new(186, 85, 211),
            [nameof(MediumPurple)] = new(147, 112, 219),
            [nameof(MediumSeaGreen)] = new(60, 179, 113),
            [nameof(MediumSlateBlue)] = new(123, 104, 238),
            [nameof(MediumSpringGreen)] = new(0, 250, 154),
            [nameof(MediumTurquoise)] = new(72, 209, 204),
            [nameof(MediumVioletRed)] = new(199, 21, 133),
            [nameof(MidnightBlue)] = new(25, 25, 112),
            [nameof(MintCream)] = new(245, 255, 250),
            [nameof(MistyRose)] = new(255, 228, 225),
            [nameof(Moccasin)] = new(255, 228, 181),
            [nameof(NavajoWhite)] = new(255, 222, 173),
            [nameof(Navy)] = new(0, 0, 128),
            [nameof(OldLace)] = new(253, 245, 230),
            [nameof(Olive)] = new(128, 128, 0),
            [nameof(OliveDrab)] = new(107, 142, 35),
            [nameof(Orange)] = new(255, 165, 0),
            [nameof(OrangeRed)] = new(255, 69, 0),
            [nameof(Orchid)] = new(218, 112, 214),
            [nameof(PaleGoldenrod)] = new(238, 232, 170),
            [nameof(PaleGreen)] = new(152, 251, 152),
            [nameof(PaleTurquoise)] = new(175, 238, 238),
            [nameof(PaleVioletRed)] = new(219, 112, 147),
            [nameof(PapayaWhip)] = new(255, 239, 213),
            [nameof(PeachPuff)] = new(255, 218, 185),
            [nameof(Peru)] = new(205, 133, 63),
            [nameof(Pink)] = new(255, 192, 203),
            [nameof(Plum)] = new(221, 160, 221),
            [nameof(PowderBlue)] = new(176, 224, 230),
            [nameof(Purple)] = new(128, 0, 128),
            [nameof(RebeccaPurple)] = new(102, 51, 153),
            [nameof(Red)] = new(255, 0, 0),
            [nameof(RosyBrown)] = new(188, 143, 143),
            [nameof(RoyalBlue)] = new(65, 105, 225),
            [nameof(SaddleBrown)] = new(139, 69, 19),
            [nameof(Salmon)] = new(250, 128, 114),
            [nameof(SandyBrown)] = new(244, 164, 96),
            [nameof(SeaGreen)] = new(46, 139, 87),
            [nameof(SeaShell)] = new(255, 245, 238),
            [nameof(Sienna)] = new(160, 82, 45),
            [nameof(Silver)] = new(192, 192, 192),
            [nameof(SkyBlue)] = new(135, 206, 235),
            [nameof(SlateBlue)] = new(106, 90, 205),
            [nameof(SlateGray)] = new(112, 128, 144),
            [nameof(SlateGrey)] = new(112, 128, 144),
            [nameof(Snow)] = new(255, 250, 250),
            [nameof(SpringGreen)] = new(0, 255, 127),
            [nameof(SteelBlue)] = new(70, 130, 180),
            [nameof(Tan)] = new(210, 180, 140),
            [nameof(Teal)] = new(0, 128, 128),
            [nameof(Thistle)] = new(216, 191, 216),
            [nameof(Tomato)] = new(255, 99, 71),
            [nameof(Turquoise)] = new(64, 224, 208),
            [nameof(Violet)] = new(238, 130, 238),
            [nameof(Wheat)] = new(245, 222, 179),
            [nameof(White)] = new(255, 255, 255),
            [nameof(WhiteSmoke)] = new(245, 245, 245),
            [nameof(Yellow)] = new(255, 255, 0),
            [nameof(YellowGreen)] = new(154, 205, 50),
        };

    /// <summary>CSS <c>aliceblue</c> (#F0F8FF).</summary>
    public static ImageColor AliceBlue => NamedColors[nameof(AliceBlue)];

    /// <summary>CSS <c>antiquewhite</c> (#FAEBD7).</summary>
    public static ImageColor AntiqueWhite => NamedColors[nameof(AntiqueWhite)];

    /// <summary>CSS <c>aqua</c> (#00FFFF).</summary>
    public static ImageColor Aqua => NamedColors[nameof(Aqua)];

    /// <summary>CSS <c>aquamarine</c> (#7FFFD4).</summary>
    public static ImageColor Aquamarine => NamedColors[nameof(Aquamarine)];

    /// <summary>CSS <c>azure</c> (#F0FFFF).</summary>
    public static ImageColor Azure => NamedColors[nameof(Azure)];

    /// <summary>CSS <c>beige</c> (#F5F5DC).</summary>
    public static ImageColor Beige => NamedColors[nameof(Beige)];

    /// <summary>CSS <c>bisque</c> (#FFE4C4).</summary>
    public static ImageColor Bisque => NamedColors[nameof(Bisque)];

    /// <summary>CSS <c>black</c> (#000000).</summary>
    public static ImageColor Black => NamedColors[nameof(Black)];

    /// <summary>CSS <c>blanchedalmond</c> (#FFEBCD).</summary>
    public static ImageColor BlanchedAlmond => NamedColors[nameof(BlanchedAlmond)];

    /// <summary>CSS <c>blue</c> (#0000FF).</summary>
    public static ImageColor Blue => NamedColors[nameof(Blue)];

    /// <summary>CSS <c>blueviolet</c> (#8A2BE2).</summary>
    public static ImageColor BlueViolet => NamedColors[nameof(BlueViolet)];

    /// <summary>CSS <c>brown</c> (#A52A2A).</summary>
    public static ImageColor Brown => NamedColors[nameof(Brown)];

    /// <summary>CSS <c>burlywood</c> (#DEB887).</summary>
    public static ImageColor BurlyWood => NamedColors[nameof(BurlyWood)];

    /// <summary>CSS <c>cadetblue</c> (#5F9EA0).</summary>
    public static ImageColor CadetBlue => NamedColors[nameof(CadetBlue)];

    /// <summary>CSS <c>chartreuse</c> (#7FFF00).</summary>
    public static ImageColor Chartreuse => NamedColors[nameof(Chartreuse)];

    /// <summary>CSS <c>chocolate</c> (#D2691E).</summary>
    public static ImageColor Chocolate => NamedColors[nameof(Chocolate)];

    /// <summary>CSS <c>coral</c> (#FF7F50).</summary>
    public static ImageColor Coral => NamedColors[nameof(Coral)];

    /// <summary>CSS <c>cornflowerblue</c> (#6495ED).</summary>
    public static ImageColor CornflowerBlue => NamedColors[nameof(CornflowerBlue)];

    /// <summary>CSS <c>cornsilk</c> (#FFF8DC).</summary>
    public static ImageColor Cornsilk => NamedColors[nameof(Cornsilk)];

    /// <summary>CSS <c>crimson</c> (#DC143C).</summary>
    public static ImageColor Crimson => NamedColors[nameof(Crimson)];

    /// <summary>CSS <c>cyan</c> (#00FFFF).</summary>
    public static ImageColor Cyan => NamedColors[nameof(Cyan)];

    /// <summary>CSS <c>darkblue</c> (#00008B).</summary>
    public static ImageColor DarkBlue => NamedColors[nameof(DarkBlue)];

    /// <summary>CSS <c>darkcyan</c> (#008B8B).</summary>
    public static ImageColor DarkCyan => NamedColors[nameof(DarkCyan)];

    /// <summary>CSS <c>darkgoldenrod</c> (#B8860B).</summary>
    public static ImageColor DarkGoldenrod => NamedColors[nameof(DarkGoldenrod)];

    /// <summary>CSS <c>darkgray</c> (#A9A9A9).</summary>
    public static ImageColor DarkGray => NamedColors[nameof(DarkGray)];

    /// <summary>CSS <c>darkgreen</c> (#006400).</summary>
    public static ImageColor DarkGreen => NamedColors[nameof(DarkGreen)];

    /// <summary>CSS <c>darkgrey</c> (#A9A9A9).</summary>
    public static ImageColor DarkGrey => NamedColors[nameof(DarkGrey)];

    /// <summary>CSS <c>darkkhaki</c> (#BDB76B).</summary>
    public static ImageColor DarkKhaki => NamedColors[nameof(DarkKhaki)];

    /// <summary>CSS <c>darkmagenta</c> (#8B008B).</summary>
    public static ImageColor DarkMagenta => NamedColors[nameof(DarkMagenta)];

    /// <summary>CSS <c>darkolivegreen</c> (#556B2F).</summary>
    public static ImageColor DarkOliveGreen => NamedColors[nameof(DarkOliveGreen)];

    /// <summary>CSS <c>darkorange</c> (#FF8C00).</summary>
    public static ImageColor DarkOrange => NamedColors[nameof(DarkOrange)];

    /// <summary>CSS <c>darkorchid</c> (#9932CC).</summary>
    public static ImageColor DarkOrchid => NamedColors[nameof(DarkOrchid)];

    /// <summary>CSS <c>darkred</c> (#8B0000).</summary>
    public static ImageColor DarkRed => NamedColors[nameof(DarkRed)];

    /// <summary>CSS <c>darksalmon</c> (#E9967A).</summary>
    public static ImageColor DarkSalmon => NamedColors[nameof(DarkSalmon)];

    /// <summary>CSS <c>darkseagreen</c> (#8FBC8F).</summary>
    public static ImageColor DarkSeaGreen => NamedColors[nameof(DarkSeaGreen)];

    /// <summary>CSS <c>darkslateblue</c> (#483D8B).</summary>
    public static ImageColor DarkSlateBlue => NamedColors[nameof(DarkSlateBlue)];

    /// <summary>CSS <c>darkslategray</c> (#2F4F4F).</summary>
    public static ImageColor DarkSlateGray => NamedColors[nameof(DarkSlateGray)];

    /// <summary>CSS <c>darkslategrey</c> (#2F4F4F).</summary>
    public static ImageColor DarkSlateGrey => NamedColors[nameof(DarkSlateGrey)];

    /// <summary>CSS <c>darkturquoise</c> (#00CED1).</summary>
    public static ImageColor DarkTurquoise => NamedColors[nameof(DarkTurquoise)];

    /// <summary>CSS <c>darkviolet</c> (#9400D3).</summary>
    public static ImageColor DarkViolet => NamedColors[nameof(DarkViolet)];

    /// <summary>CSS <c>deeppink</c> (#FF1493).</summary>
    public static ImageColor DeepPink => NamedColors[nameof(DeepPink)];

    /// <summary>CSS <c>deepskyblue</c> (#00BFFF).</summary>
    public static ImageColor DeepSkyBlue => NamedColors[nameof(DeepSkyBlue)];

    /// <summary>CSS <c>dimgray</c> (#696969).</summary>
    public static ImageColor DimGray => NamedColors[nameof(DimGray)];

    /// <summary>CSS <c>dimgrey</c> (#696969).</summary>
    public static ImageColor DimGrey => NamedColors[nameof(DimGrey)];

    /// <summary>CSS <c>dodgerblue</c> (#1E90FF).</summary>
    public static ImageColor DodgerBlue => NamedColors[nameof(DodgerBlue)];

    /// <summary>CSS <c>firebrick</c> (#B22222).</summary>
    public static ImageColor Firebrick => NamedColors[nameof(Firebrick)];

    /// <summary>CSS <c>floralwhite</c> (#FFFAF0).</summary>
    public static ImageColor FloralWhite => NamedColors[nameof(FloralWhite)];

    /// <summary>CSS <c>forestgreen</c> (#228B22).</summary>
    public static ImageColor ForestGreen => NamedColors[nameof(ForestGreen)];

    /// <summary>CSS <c>fuchsia</c> (#FF00FF).</summary>
    public static ImageColor Fuchsia => NamedColors[nameof(Fuchsia)];

    /// <summary>CSS <c>gainsboro</c> (#DCDCDC).</summary>
    public static ImageColor Gainsboro => NamedColors[nameof(Gainsboro)];

    /// <summary>CSS <c>ghostwhite</c> (#F8F8FF).</summary>
    public static ImageColor GhostWhite => NamedColors[nameof(GhostWhite)];

    /// <summary>CSS <c>gold</c> (#FFD700).</summary>
    public static ImageColor Gold => NamedColors[nameof(Gold)];

    /// <summary>CSS <c>goldenrod</c> (#DAA520).</summary>
    public static ImageColor Goldenrod => NamedColors[nameof(Goldenrod)];

    /// <summary>CSS <c>gray</c> (#808080).</summary>
    public static ImageColor Gray => NamedColors[nameof(Gray)];

    /// <summary>CSS <c>green</c> (#008000).</summary>
    public static ImageColor Green => NamedColors[nameof(Green)];

    /// <summary>CSS <c>greenyellow</c> (#ADFF2F).</summary>
    public static ImageColor GreenYellow => NamedColors[nameof(GreenYellow)];

    /// <summary>CSS <c>grey</c> (#808080).</summary>
    public static ImageColor Grey => NamedColors[nameof(Grey)];

    /// <summary>CSS <c>honeydew</c> (#F0FFF0).</summary>
    public static ImageColor Honeydew => NamedColors[nameof(Honeydew)];

    /// <summary>CSS <c>hotpink</c> (#FF69B4).</summary>
    public static ImageColor HotPink => NamedColors[nameof(HotPink)];

    /// <summary>CSS <c>indianred</c> (#CD5C5C).</summary>
    public static ImageColor IndianRed => NamedColors[nameof(IndianRed)];

    /// <summary>CSS <c>indigo</c> (#4B0082).</summary>
    public static ImageColor Indigo => NamedColors[nameof(Indigo)];

    /// <summary>CSS <c>ivory</c> (#FFFFF0).</summary>
    public static ImageColor Ivory => NamedColors[nameof(Ivory)];

    /// <summary>CSS <c>khaki</c> (#F0E68C).</summary>
    public static ImageColor Khaki => NamedColors[nameof(Khaki)];

    /// <summary>CSS <c>lavender</c> (#E6E6FA).</summary>
    public static ImageColor Lavender => NamedColors[nameof(Lavender)];

    /// <summary>CSS <c>lavenderblush</c> (#FFF0F5).</summary>
    public static ImageColor LavenderBlush => NamedColors[nameof(LavenderBlush)];

    /// <summary>CSS <c>lawngreen</c> (#7CFC00).</summary>
    public static ImageColor LawnGreen => NamedColors[nameof(LawnGreen)];

    /// <summary>CSS <c>lemonchiffon</c> (#FFFACD).</summary>
    public static ImageColor LemonChiffon => NamedColors[nameof(LemonChiffon)];

    /// <summary>CSS <c>lightblue</c> (#ADD8E6).</summary>
    public static ImageColor LightBlue => NamedColors[nameof(LightBlue)];

    /// <summary>CSS <c>lightcoral</c> (#F08080).</summary>
    public static ImageColor LightCoral => NamedColors[nameof(LightCoral)];

    /// <summary>CSS <c>lightcyan</c> (#E0FFFF).</summary>
    public static ImageColor LightCyan => NamedColors[nameof(LightCyan)];

    /// <summary>CSS <c>lightgoldenrodyellow</c> (#FAFAD2).</summary>
    public static ImageColor LightGoldenrodYellow => NamedColors[nameof(LightGoldenrodYellow)];

    /// <summary>CSS <c>lightgray</c> (#D3D3D3).</summary>
    public static ImageColor LightGray => NamedColors[nameof(LightGray)];

    /// <summary>CSS <c>lightgreen</c> (#90EE90).</summary>
    public static ImageColor LightGreen => NamedColors[nameof(LightGreen)];

    /// <summary>CSS <c>lightgrey</c> (#D3D3D3).</summary>
    public static ImageColor LightGrey => NamedColors[nameof(LightGrey)];

    /// <summary>CSS <c>lightpink</c> (#FFB6C1).</summary>
    public static ImageColor LightPink => NamedColors[nameof(LightPink)];

    /// <summary>CSS <c>lightsalmon</c> (#FFA07A).</summary>
    public static ImageColor LightSalmon => NamedColors[nameof(LightSalmon)];

    /// <summary>CSS <c>lightseagreen</c> (#20B2AA).</summary>
    public static ImageColor LightSeaGreen => NamedColors[nameof(LightSeaGreen)];

    /// <summary>CSS <c>lightskyblue</c> (#87CEFA).</summary>
    public static ImageColor LightSkyBlue => NamedColors[nameof(LightSkyBlue)];

    /// <summary>CSS <c>lightslategray</c> (#778899).</summary>
    public static ImageColor LightSlateGray => NamedColors[nameof(LightSlateGray)];

    /// <summary>CSS <c>lightslategrey</c> (#778899).</summary>
    public static ImageColor LightSlateGrey => NamedColors[nameof(LightSlateGrey)];

    /// <summary>CSS <c>lightsteelblue</c> (#B0C4DE).</summary>
    public static ImageColor LightSteelBlue => NamedColors[nameof(LightSteelBlue)];

    /// <summary>CSS <c>lightyellow</c> (#FFFFE0).</summary>
    public static ImageColor LightYellow => NamedColors[nameof(LightYellow)];

    /// <summary>CSS <c>lime</c> (#00FF00).</summary>
    public static ImageColor Lime => NamedColors[nameof(Lime)];

    /// <summary>CSS <c>limegreen</c> (#32CD32).</summary>
    public static ImageColor LimeGreen => NamedColors[nameof(LimeGreen)];

    /// <summary>CSS <c>linen</c> (#FAF0E6).</summary>
    public static ImageColor Linen => NamedColors[nameof(Linen)];

    /// <summary>CSS <c>magenta</c> (#FF00FF).</summary>
    public static ImageColor Magenta => NamedColors[nameof(Magenta)];

    /// <summary>CSS <c>maroon</c> (#800000).</summary>
    public static ImageColor Maroon => NamedColors[nameof(Maroon)];

    /// <summary>CSS <c>mediumaquamarine</c> (#66CDAA).</summary>
    public static ImageColor MediumAquamarine => NamedColors[nameof(MediumAquamarine)];

    /// <summary>CSS <c>mediumblue</c> (#0000CD).</summary>
    public static ImageColor MediumBlue => NamedColors[nameof(MediumBlue)];

    /// <summary>CSS <c>mediumorchid</c> (#BA55D3).</summary>
    public static ImageColor MediumOrchid => NamedColors[nameof(MediumOrchid)];

    /// <summary>CSS <c>mediumpurple</c> (#9370DB).</summary>
    public static ImageColor MediumPurple => NamedColors[nameof(MediumPurple)];

    /// <summary>CSS <c>mediumseagreen</c> (#3CB371).</summary>
    public static ImageColor MediumSeaGreen => NamedColors[nameof(MediumSeaGreen)];

    /// <summary>CSS <c>mediumslateblue</c> (#7B68EE).</summary>
    public static ImageColor MediumSlateBlue => NamedColors[nameof(MediumSlateBlue)];

    /// <summary>CSS <c>mediumspringgreen</c> (#00FA9A).</summary>
    public static ImageColor MediumSpringGreen => NamedColors[nameof(MediumSpringGreen)];

    /// <summary>CSS <c>mediumturquoise</c> (#48D1CC).</summary>
    public static ImageColor MediumTurquoise => NamedColors[nameof(MediumTurquoise)];

    /// <summary>CSS <c>mediumvioletred</c> (#C71585).</summary>
    public static ImageColor MediumVioletRed => NamedColors[nameof(MediumVioletRed)];

    /// <summary>CSS <c>midnightblue</c> (#191970).</summary>
    public static ImageColor MidnightBlue => NamedColors[nameof(MidnightBlue)];

    /// <summary>CSS <c>mintcream</c> (#F5FFFA).</summary>
    public static ImageColor MintCream => NamedColors[nameof(MintCream)];

    /// <summary>CSS <c>mistyrose</c> (#FFE4E1).</summary>
    public static ImageColor MistyRose => NamedColors[nameof(MistyRose)];

    /// <summary>CSS <c>moccasin</c> (#FFE4B5).</summary>
    public static ImageColor Moccasin => NamedColors[nameof(Moccasin)];

    /// <summary>CSS <c>navajowhite</c> (#FFDEAD).</summary>
    public static ImageColor NavajoWhite => NamedColors[nameof(NavajoWhite)];

    /// <summary>CSS <c>navy</c> (#000080).</summary>
    public static ImageColor Navy => NamedColors[nameof(Navy)];

    /// <summary>CSS <c>oldlace</c> (#FDF5E6).</summary>
    public static ImageColor OldLace => NamedColors[nameof(OldLace)];

    /// <summary>CSS <c>olive</c> (#808000).</summary>
    public static ImageColor Olive => NamedColors[nameof(Olive)];

    /// <summary>CSS <c>olivedrab</c> (#6B8E23).</summary>
    public static ImageColor OliveDrab => NamedColors[nameof(OliveDrab)];

    /// <summary>CSS <c>orange</c> (#FFA500).</summary>
    public static ImageColor Orange => NamedColors[nameof(Orange)];

    /// <summary>CSS <c>orangered</c> (#FF4500).</summary>
    public static ImageColor OrangeRed => NamedColors[nameof(OrangeRed)];

    /// <summary>CSS <c>orchid</c> (#DA70D6).</summary>
    public static ImageColor Orchid => NamedColors[nameof(Orchid)];

    /// <summary>CSS <c>palegoldenrod</c> (#EEE8AA).</summary>
    public static ImageColor PaleGoldenrod => NamedColors[nameof(PaleGoldenrod)];

    /// <summary>CSS <c>palegreen</c> (#98FB98).</summary>
    public static ImageColor PaleGreen => NamedColors[nameof(PaleGreen)];

    /// <summary>CSS <c>paleturquoise</c> (#AFEEEE).</summary>
    public static ImageColor PaleTurquoise => NamedColors[nameof(PaleTurquoise)];

    /// <summary>CSS <c>palevioletred</c> (#DB7093).</summary>
    public static ImageColor PaleVioletRed => NamedColors[nameof(PaleVioletRed)];

    /// <summary>CSS <c>papayawhip</c> (#FFEFD5).</summary>
    public static ImageColor PapayaWhip => NamedColors[nameof(PapayaWhip)];

    /// <summary>CSS <c>peachpuff</c> (#FFDAB9).</summary>
    public static ImageColor PeachPuff => NamedColors[nameof(PeachPuff)];

    /// <summary>CSS <c>peru</c> (#CD853F).</summary>
    public static ImageColor Peru => NamedColors[nameof(Peru)];

    /// <summary>CSS <c>pink</c> (#FFC0CB).</summary>
    public static ImageColor Pink => NamedColors[nameof(Pink)];

    /// <summary>CSS <c>plum</c> (#DDA0DD).</summary>
    public static ImageColor Plum => NamedColors[nameof(Plum)];

    /// <summary>CSS <c>powderblue</c> (#B0E0E6).</summary>
    public static ImageColor PowderBlue => NamedColors[nameof(PowderBlue)];

    /// <summary>CSS <c>purple</c> (#800080).</summary>
    public static ImageColor Purple => NamedColors[nameof(Purple)];

    /// <summary>CSS <c>rebeccapurple</c> (#663399).</summary>
    public static ImageColor RebeccaPurple => NamedColors[nameof(RebeccaPurple)];

    /// <summary>CSS <c>red</c> (#FF0000).</summary>
    public static ImageColor Red => NamedColors[nameof(Red)];

    /// <summary>CSS <c>rosybrown</c> (#BC8F8F).</summary>
    public static ImageColor RosyBrown => NamedColors[nameof(RosyBrown)];

    /// <summary>CSS <c>royalblue</c> (#4169E1).</summary>
    public static ImageColor RoyalBlue => NamedColors[nameof(RoyalBlue)];

    /// <summary>CSS <c>saddlebrown</c> (#8B4513).</summary>
    public static ImageColor SaddleBrown => NamedColors[nameof(SaddleBrown)];

    /// <summary>CSS <c>salmon</c> (#FA8072).</summary>
    public static ImageColor Salmon => NamedColors[nameof(Salmon)];

    /// <summary>CSS <c>sandybrown</c> (#F4A460).</summary>
    public static ImageColor SandyBrown => NamedColors[nameof(SandyBrown)];

    /// <summary>CSS <c>seagreen</c> (#2E8B57).</summary>
    public static ImageColor SeaGreen => NamedColors[nameof(SeaGreen)];

    /// <summary>CSS <c>seashell</c> (#FFF5EE).</summary>
    public static ImageColor SeaShell => NamedColors[nameof(SeaShell)];

    /// <summary>CSS <c>sienna</c> (#A0522D).</summary>
    public static ImageColor Sienna => NamedColors[nameof(Sienna)];

    /// <summary>CSS <c>silver</c> (#C0C0C0).</summary>
    public static ImageColor Silver => NamedColors[nameof(Silver)];

    /// <summary>CSS <c>skyblue</c> (#87CEEB).</summary>
    public static ImageColor SkyBlue => NamedColors[nameof(SkyBlue)];

    /// <summary>CSS <c>slateblue</c> (#6A5ACD).</summary>
    public static ImageColor SlateBlue => NamedColors[nameof(SlateBlue)];

    /// <summary>CSS <c>slategray</c> (#708090).</summary>
    public static ImageColor SlateGray => NamedColors[nameof(SlateGray)];

    /// <summary>CSS <c>slategrey</c> (#708090).</summary>
    public static ImageColor SlateGrey => NamedColors[nameof(SlateGrey)];

    /// <summary>CSS <c>snow</c> (#FFFAFA).</summary>
    public static ImageColor Snow => NamedColors[nameof(Snow)];

    /// <summary>CSS <c>springgreen</c> (#00FF7F).</summary>
    public static ImageColor SpringGreen => NamedColors[nameof(SpringGreen)];

    /// <summary>CSS <c>steelblue</c> (#4682B4).</summary>
    public static ImageColor SteelBlue => NamedColors[nameof(SteelBlue)];

    /// <summary>CSS <c>tan</c> (#D2B48C).</summary>
    public static ImageColor Tan => NamedColors[nameof(Tan)];

    /// <summary>CSS <c>teal</c> (#008080).</summary>
    public static ImageColor Teal => NamedColors[nameof(Teal)];

    /// <summary>CSS <c>thistle</c> (#D8BFD8).</summary>
    public static ImageColor Thistle => NamedColors[nameof(Thistle)];

    /// <summary>CSS <c>tomato</c> (#FF6347).</summary>
    public static ImageColor Tomato => NamedColors[nameof(Tomato)];

    /// <summary>CSS <c>turquoise</c> (#40E0D0).</summary>
    public static ImageColor Turquoise => NamedColors[nameof(Turquoise)];

    /// <summary>CSS <c>violet</c> (#EE82EE).</summary>
    public static ImageColor Violet => NamedColors[nameof(Violet)];

    /// <summary>CSS <c>wheat</c> (#F5DEB3).</summary>
    public static ImageColor Wheat => NamedColors[nameof(Wheat)];

    /// <summary>CSS <c>white</c> (#FFFFFF).</summary>
    public static ImageColor White => NamedColors[nameof(White)];

    /// <summary>CSS <c>whitesmoke</c> (#F5F5F5).</summary>
    public static ImageColor WhiteSmoke => NamedColors[nameof(WhiteSmoke)];

    /// <summary>CSS <c>yellow</c> (#FFFF00).</summary>
    public static ImageColor Yellow => NamedColors[nameof(Yellow)];

    /// <summary>CSS <c>yellowgreen</c> (#9ACD32).</summary>
    public static ImageColor YellowGreen => NamedColors[nameof(YellowGreen)];
}
