using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using BS.GIS.MapConvert;
using BS.GIS.MapTools;
using DispatchScreenStats.Models;
using Oracle.ManagedDataAccess.Client;

namespace DispatchScreenStats.Common
{
    public class OracleHelper
    {
        public object ExecuteScalar(string sql)
        {
            var conn = new OracleConnection(ConfigurationManager.ConnectionStrings["OracleConn"].ConnectionString);
            conn.Open();
            var cmd = new OracleCommand(sql, conn);
            var res = cmd.ExecuteScalar();
            conn.Close();
            return res;
        }

        public Point GetPointByLine(string line, string station)
        {
            var conn = new OracleConnection(ConfigurationManager.ConnectionStrings["OracleConn"].ConnectionString);
            conn.Open();
            var cmd =
                new OracleCommand(
                    string.Format(
                        "select strlon02,strlat02 from stationclass t where levelid=1 and roadline='{0}' and (stationname like '%{1}%' or levelname like '%{1}%')",
                        line, station.Substring(0, station.Length - 2)),
                    conn);
            var dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            if (!dataReader.HasRows) return null;
            dataReader.Read();
            var gpsPoint =
                GPSConvert.Gcj02_To_Bd09(new GPSPoint((double) dataReader["strlon02"], (double) dataReader["strlat02"]));
            var point = new Point(gpsPoint.Lon.ToString(CultureInfo.InvariantCulture),
                gpsPoint.Lat.ToString(CultureInfo.InvariantCulture));
            dataReader.Close();
            cmd.Dispose();
            conn.Close();
            return point;
        }

        public List<dynamic> GetPointsByLines(List<ScreenRec> screens)
        {
            var res = new List<dynamic>();
            var lines =
                screens.Select(
                        t =>
                            new
                            {
                                rec = t,
                                line = t.LineName.Contains("、")? t.LineName.Split(new[] {'、'}, StringSplitOptions.RemoveEmptyEntries)[0]:t.LineName
                            })
                    .ToList();
            string inStr;
            if (lines.Count > 1000)
            {
                inStr = "roadline in (" + string.Join(",", lines.Take(1000).Select(t => "'" + t.line + "'").ToArray()) +
                        ")";
                var i = 1;
                while (lines.Skip(1000 * i).Any())
                {
                    var l = lines.Skip(1000 * i).ToList();
                    inStr += " or roadline in (" +
                             string.Join(",",
                                 l.Take(l.Count > 1000 ? 1000 : l.Count).Select(t => "'" + t.line + "'").ToArray()) +
                             ")";
                    i++;
                }
            }
            else
            {
                inStr = "roadline in (" + string.Join(",", lines.Select(t => "'" + t.line + "'").ToArray()) + ")";
            }

            var conn = new OracleConnection(ConfigurationManager.ConnectionStrings["OracleConn"].ConnectionString);
            conn.Open();
            var cmd =
                new OracleCommand(
                    string.Format(
                        "select roadline,stationname,levelname,strlon02,strlat02 from stationclass t where levelid=1 and ({0})",
                        inStr), conn);
            var dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            if (!dataReader.HasRows) return null;
            var dt = new DataTable();
            dt.Load(dataReader);
            foreach (var line in lines)
            {
                var nameLike = line.rec.InstallStation.Length > 0
                    ? line.rec.InstallStation.Substring(0, line.rec.InstallStation.Length - 2)
                    : "";
                var dr =
                    dt.Select("roadline='" + line.line + "' and (stationname like '%" +nameLike+ "%' or levelname like '%"+nameLike+"%')")
                        .FirstOrDefault();
                if (dr == null)
                {
                    continue;
                }
                var gpsPoint =
                    GPSConvert.Gcj02_To_Bd09(new GPSPoint((double) dr["strlon02"], (double) dr["strlat02"]));
                var point = new Point(gpsPoint.Lon.ToString(CultureInfo.InvariantCulture),
                    gpsPoint.Lat.ToString(CultureInfo.InvariantCulture));
                res.Add(new {line.rec, point});
            }
            dataReader.Close();
            cmd.Dispose();
            conn.Close();

            return res;
        }
    }
}