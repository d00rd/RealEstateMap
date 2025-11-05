
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RealEstateMap
{
    public class MapPickerForm : Form
    {
        private readonly PictureBox pbMap;
        private readonly Label lblCoords;
        private readonly Button btnUse;
        private readonly Button btnCancel;
        private readonly HttpClient httpClient = new HttpClient();
        private Bitmap mapImage;
        private double centerLat;
        private double centerLon;
        private int zoom;
        private double topLeftWorldPx;
        private double topLeftWorldPy;
        private const int TileSize = 256;
        private readonly int mapWidth;
        private readonly int mapHeight;
        private double? selectedLat;
        private double? selectedLon;

        public double SelectedLatitude => selectedLat ?? 0;
        public double SelectedLongitude => selectedLon ?? 0;

        public MapPickerForm(double initialLat = 45.7580, double initialLon = 21.2355, int initialZoom = 15, int width = 600, int height = 400)
        {
            Text = "Choose location on map";
            StartPosition = FormStartPosition.CenterParent;
            mapWidth = width;
            mapHeight = height;
            Size = new Size(mapWidth + 40, mapHeight + 120);
            centerLat = initialLat;
            centerLon = initialLon;
            zoom = initialZoom;

            pbMap = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(mapWidth, mapHeight),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Normal
            };
            pbMap.MouseClick += PbMap_MouseClick;
            Controls.Add(pbMap);

            lblCoords = new Label
            {
                Location = new Point(10, mapHeight + 20),
                Size = new Size(mapWidth, 20),
                Text = $"Lat: {centerLat.ToString(CultureInfo.InvariantCulture)}, Lon: {centerLon.ToString(CultureInfo.InvariantCulture)}"
            };
            Controls.Add(lblCoords);

            btnUse = new Button { Location = new Point(10, mapHeight + 50), Size = new Size(120, 30), Text = "Use selection" };
            btnUse.Click += BtnUse_Click;
            Controls.Add(btnUse);

            btnCancel = new Button { Location = new Point(140, mapHeight + 50), Size = new Size(120, 30), Text = "Cancel" };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RealEstateAgency/1.0");

            this.Load += async (s, e) => { await GenerateMapAsync(centerLat, centerLon, zoom); };
            this.FormClosing += (s, e) => { mapImage?.Dispose(); httpClient.Dispose(); };
        }

        private async Task GenerateMapAsync(double latitude, double longitude, int z)
        {
            // Dispose previous
            mapImage?.Dispose();
            mapImage = new Bitmap(mapWidth, mapHeight);

            using (Graphics g = Graphics.FromImage(mapImage))
            {
                g.Clear(Color.LightGray);

                var (centerPx, centerPy) = LatLonToWorldPixels(latitude, longitude, z);

                topLeftWorldPx = centerPx - mapWidth / 2.0;
                topLeftWorldPy = centerPy - mapHeight / 2.0;

                int firstTileX = (int)Math.Floor(topLeftWorldPx / TileSize);
                int firstTileY = (int)Math.Floor(topLeftWorldPy / TileSize);
                int lastTileX = (int)Math.Floor((topLeftWorldPx + mapWidth) / TileSize);
                int lastTileY = (int)Math.Floor((topLeftWorldPy + mapHeight) / TileSize);

                int maxTile = (int)Math.Pow(2, z);

                for (int tx = firstTileX; tx <= lastTileX; tx++)
                {
                    for (int ty = firstTileY; ty <= lastTileY; ty++)
                    {
                        int wrappedTx = tx;
                        if (wrappedTx < 0) wrappedTx = (wrappedTx % maxTile + maxTile) % maxTile;
                        if (wrappedTx >= maxTile) wrappedTx = wrappedTx % maxTile;
                        if (ty < 0 || ty >= maxTile) continue;

                        string tileUrl = $"https://tile.openstreetmap.org/{z}/{wrappedTx}/{ty}.png";

                        try
                        {
                            byte[] tileData = await httpClient.GetByteArrayAsync(tileUrl);
                            using (var ms = new MemoryStream(tileData))
                            using (Image tileImage = Image.FromStream(ms))
                            {
                                double tileWorldPx = tx * TileSize;
                                double tileWorldPy = ty * TileSize;

                                int drawX = (int)Math.Round(tileWorldPx - topLeftWorldPx);
                                int drawY = (int)Math.Round(tileWorldPy - topLeftWorldPy);

                                g.DrawImage(tileImage, drawX, drawY, TileSize, TileSize);
                            }
                        }
                        catch
                        {
                            double tileWorldPx = tx * TileSize;
                            double tileWorldPy = ty * TileSize;
                            int drawX = (int)Math.Round(tileWorldPx - topLeftWorldPx);
                            int drawY = (int)Math.Round(tileWorldPy - topLeftWorldPy);
                            g.FillRectangle(Brushes.Gray, drawX, drawY, TileSize, TileSize);
                        }
                    }
                }
            }

            pbMap.Image = (Bitmap)mapImage.Clone();

            // reset selection
            selectedLat = null;
            selectedLon = null;
            lblCoords.Text = $"Center: {latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}";
        }

        private void PbMap_MouseClick(object? sender, MouseEventArgs e)
        {
            // compute world pixel of click
            double worldPx = topLeftWorldPx + e.X;
            double worldPy = topLeftWorldPy + e.Y;

            double mapSize = TileSize * Math.Pow(2, zoom);

            double lon = worldPx / mapSize * 360.0 - 180.0;
            double yNorm = worldPy / mapSize; // 0..1
            double lat = InverseMercatorLat(yNorm);

            selectedLat = lat;
            selectedLon = lon;

            // redraw overlay
            RedrawMapWithMarker((int)Math.Round((decimal)e.X), (int)Math.Round((decimal)e.Y));
            lblCoords.Text = $"Lat: {lat.ToString(CultureInfo.InvariantCulture)}, Lon: {lon.ToString(CultureInfo.InvariantCulture)}";
        }

        private void RedrawMapWithMarker(int x, int y)
        {
            // Copy base image and draw marker
            pbMap.Image?.Dispose();
            var bmp = (Bitmap)mapImage.Clone();
            using (Graphics g = Graphics.FromImage(bmp))
            {
                int r = 8;
                using (Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.FillEllipse(shadow, x - r + 2, y - r + 2, r * 2, r * 2);
                }
                using (Brush red = new SolidBrush(Color.Red))
                using (Pen border = new Pen(Color.DarkRed, 2))
                {
                    g.FillEllipse(red, x - r, y - r, r * 2, r * 2);
                    g.DrawEllipse(border, x - r, y - r, r * 2, r * 2);
                }
            }
            pbMap.Image = bmp;
        }

        private void BtnUse_Click(object? sender, EventArgs e)
        {
            if (!selectedLat.HasValue || !selectedLon.HasValue)
            {
                MessageBox.Show("Click the map to choose a location first.", "No selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private static (double px, double py) LatLonToWorldPixels(double lat, double lon, int zoomLevel)
        {
            double mapSize = TileSize * Math.Pow(2, zoomLevel);
            double x = (lon + 180.0) / 360.0 * mapSize;

            double sinLat = Math.Sin(lat * Math.PI / 180.0);
            sinLat = Math.Min(Math.Max(sinLat, -0.9999), 0.9999);
            double y = (0.5 - Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI)) * mapSize;

            return (x, y);
        }

        private static double InverseMercatorLat(double y)
        {
            double n = Math.PI * (1.0 - 2.0 * y);
            double latRad = Math.Atan(Math.Sinh(n));
            return latRad * 180.0 / Math.PI;
        }
    }
}