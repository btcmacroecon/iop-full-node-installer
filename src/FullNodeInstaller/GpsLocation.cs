using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FullNodeInstaller
{
  /// <summary>
  /// Geographic location.
  /// </summary>
  public class GpsLocation
  {
    /// <summary>Minimal value of latitude in floating point representation.</summary>
    public const decimal LatitudeMin = -90;

    /// <summary>Maximal value of latitude in floating point representation.</summary>
    public const decimal LatitudeMax = 90;

    /// <summary>Minimal value of longitude in floating point representation.</summary>
    public const decimal LongitudeMin = -180;

    /// <summary>Maximal value of longitude in floating point representation.</summary>
    public const decimal LongitudeMax = 180;

    /// <summary>GPS latitude is a number in range [-90;90].</summary>
    public decimal Latitude;

    /// <summary>GPS longitude is a number in range (-180;180].</summary>
    public decimal Longitude;

    /// <summary>
    /// Initializes GPS location information from floating point values.
    /// </summary>
    /// <param name="Latitude">Floating point latitude information. The valid range of values is [-90;90].</param>
    /// <param name="Longitude">Floating point longitude information. The valid range of values is [-180;180].</param>
    public GpsLocation(decimal Latitude, decimal Longitude)
    {
      this.Latitude = Latitude;
      this.Longitude = Longitude;
    }

    public override string ToString()
    {
      return ToString("G");
    }


    /// <summary>
    /// Formats the value of the current instance using the specified format.
    /// </summary>
    /// <param name="Format">Type of format to use. Currently only "G" and "US" is supported.</param>
    /// <returns>Formatted string.</returns>
    public string ToString(string Format)
    {
      return ToString(Format, null);
    }

    /// <summary>
    /// Formats the value of the current instance using the specified format.
    /// </summary>
    /// <param name="Format">Type of format to use. Currently only "G" and "US" is supported.</param>
    /// <param name="Provider">The provider to use to format the value.</param>
    /// <returns>Formatted string.</returns>
    public string ToString(string Format, IFormatProvider Provider)
    {
      if (string.IsNullOrEmpty(Format)) Format = "G";
      Format = Format.Trim().ToUpperInvariant();
      if (Provider == null) Provider = CultureInfo.CurrentCulture;

      string res = "N/A";
      if (IsValid())
      {
        switch (Format)
        {
          case "G":
            res = string.Format("{0} {1}", Latitude.ToString("0.######", Provider), Longitude.ToString("0.######", Provider));
            break;

          case "US":
            {
              CultureInfo enUs = new CultureInfo("en-US");
              res = string.Format("{0}, {1}", Latitude.ToString("0.######", enUs), Longitude.ToString("0.######", enUs));
              break;
            }

          default:
            res = "Invalid format";
            break;
        }
      }

      return res;
    }


    /// <summary>
    /// Checks whether the internal values of the instance represent valid GPS location.
    /// </summary>
    /// <returns>true if the object instance represents valid GPS location, false otherwise.</returns>
    public bool IsValid()
    {
      return (LatitudeMin <= Latitude) && (Latitude <= LatitudeMax)
        && (LongitudeMin < Longitude) && (Longitude <= LongitudeMax);
    }

  }
}
