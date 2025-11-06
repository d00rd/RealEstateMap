using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RealEstateMap
{
    public partial class Form1 : Form
    {
        private PictureBox pictureBoxMap;
        private Button btnLoadMap;
        private Label lblStatus;
        private static readonly HttpClient httpClient = new HttpClient();

        private ComboBox cmbAgencies;
        private ListBox lstProperties;
        private Button btnAddProperty;
        private Button btnShowProperty;


        private List<RealEstateAgency> agencies = new List<RealEstateAgency>();

        private readonly Dictionary<Rectangle, List<Property>> markerHitboxes = new Dictionary<Rectangle, List<Property>>();

        public Form1()
        {
            InitializeComponent();
            SetupControls();

            this.Load += async (s, e) =>
            {
                agencies = await PersistenceService.LoadAsync();

                PopulateAgencies();
                RefreshProperties();
            };

     
            this.FormClosing += async (s, e) =>
            {
                try
                {
                    await PersistenceService.SaveAsync(agencies);
                }
                catch
                {
                    // ignore 
                }
            };

            httpClient.DefaultRequestHeaders.Add("User-Agent", "RealEstateAgency/1.0");
        }

        private void SetupControls()
        {
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;


            var pnl = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(260, 540)
            };
            this.Controls.Add(pnl);

            var lblAgency = new Label { Location = new Point(0, 0), Text = "Agency", Width = 120 };
            cmbAgencies = new ComboBox { Location = new Point(0, 20), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAgencies.SelectedIndexChanged += CmbAgencies_SelectedIndexChanged;

            var lblProps = new Label { Location = new Point(0, 60), Text = "Properties", Width = 120 };
            lstProperties = new ListBox { Location = new Point(0, 80), Size = new Size(250, 360) };
            lstProperties.SelectedIndexChanged += LstProperties_SelectedIndexChanged;
            lstProperties.DoubleClick += LstProperties_DoubleClick;

            btnAddProperty = new Button { Location = new Point(0, 450), Size = new Size(120, 30), Text = "Add Property" };
            btnAddProperty.Click += BtnAddProperty_Click;

            btnShowProperty = new Button { Location = new Point(130, 450), Size = new Size(120, 30), Text = "Show on Map" };
            btnShowProperty.Click += BtnShowProperty_Click;

            pnl.Controls.AddRange(new Control[] { lblAgency, cmbAgencies, lblProps, lstProperties, btnAddProperty, btnShowProperty });

            // Map (right)
            pictureBoxMap = new PictureBox
            {
                Location = new Point(280, 10),
                Size = new Size(600, 500),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Normal // important for 1:1 hit testing
            };
            this.Controls.Add(pictureBoxMap);

            pictureBoxMap.MouseClick += PictureBoxMap_MouseClick;

            lblStatus = new Label
            {
                Location = new Point(280, 520),
                Size = new Size(600, 30),
                Text = "Select an agency to view its properties on the map"
            };
            this.Controls.Add(lblStatus);

            btnLoadMap = new Button
            {
                Location = new Point(280, 560),
                Size = new Size(150, 30),
                Text = "Reload Map"
            };
            btnLoadMap.Click += BtnLoadMap_Click;
            this.Controls.Add(btnLoadMap);
        }

        private void PopulateAgencies()
        {
            cmbAgencies.Items.Clear();
            foreach (var a in agencies)
                cmbAgencies.Items.Add(a);

            cmbAgencies.DisplayMember = "Name";
            if (cmbAgencies.Items.Count > 0)
                cmbAgencies.SelectedIndex = 0;
        }

        private void CmbAgencies_SelectedIndexChanged(object? sender, EventArgs e)
        {
            RefreshProperties();

            var props = GetSelectedAgencyProperties().ToList();
            if (props.Any())
            {
                ShowAgencyOnMap(props);
            }
            else
            {
                _ = LoadMap(45.7580, 21.2355);
            }
        }

        private void RefreshProperties()
        {
            lstProperties.Items.Clear();
            var agency = cmbAgencies.SelectedItem as RealEstateAgency;
            if (agency == null) return;

            foreach (var p in agency.Properties)
                lstProperties.Items.Add(p);

            lstProperties.DisplayMember = "Address";
            lblStatus.Text = $"{agency.Name} - {agency.Properties.Count} properties";
        }


        private IEnumerable<Property> GetSelectedAgencyProperties()
        {
            var agency = cmbAgencies.SelectedItem as RealEstateAgency;
            if (agency == null) return Enumerable.Empty<Property>();
            return agency.Properties;
        }

        private async void BtnLoadMap_Click(object sender, EventArgs e)
        {
            var props = GetSelectedAgencyProperties().ToList();
            double latitude, longitude;
            int zoom = 15;
            if (props.Any())
            {
                ShowAgencyOnMap(props);
                return;
            }
            else
            {
                latitude = 45.7580;
                longitude = 21.2355;
            }

            await LoadMap(latitude, longitude, zoom);
        }

        private async void LstProperties_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var prop = lstProperties.SelectedItem as Property;
            if (prop != null)
            {
                lblStatus.Text = prop.ToString();
                await ShowSinglePropertyOnMap(prop);
            }
        }

        private async void LstProperties_DoubleClick(object? sender, EventArgs e)
        {
            var prop = lstProperties.SelectedItem as Property;
            if (prop != null)
            {
                await ShowSinglePropertyOnMap(prop);
            }
        }

        private async Task ShowSinglePropertyOnMap(Property prop)
        {
            if (prop == null) return;
            const int zoomForSingle = 17; 
            await LoadMap(prop.Latitude, prop.Longitude, zoomForSingle, new[] { prop });
        }

        private void BtnAddProperty_Click(object? sender, EventArgs e)
        {
            var agency = cmbAgencies.SelectedItem as RealEstateAgency;
            if (agency == null)
            {
                MessageBox.Show("Select an agency first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var addForm = new AddPropertyForm();
            if (addForm.ShowDialog(this) == DialogResult.OK)
            {
                var created = addForm.CreatedProperty;
                if (created != null)
                {
                    try
                    {
                        agency.AddProperty(created);
                        RefreshProperties();

                        _ = ShowSinglePropertyOnMap(created);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Add property", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void BtnShowProperty_Click(object? sender, EventArgs e)
        {
  
            var props = GetSelectedAgencyProperties().ToList();
            if (props.Any())
            {
                ShowAgencyOnMap(props);
            }
            else
            {
                MessageBox.Show("Selected agency has no properties.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private async void ShowAgencyOnMap(List<Property> properties)
        {
            if (properties == null || properties.Count == 0)
            {
                await LoadMap(45.7580, 21.2355);
                return;
            }

            const int tileSize = 256;
            const int maxZoom = 19;
            const int minZoom = 0;
            int width = pictureBoxMap.Width;
            int height = pictureBoxMap.Height;

 
            double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
            double minY = double.PositiveInfinity, maxY = double.NegativeInfinity;

            foreach (var p in properties)
            {
                double x = (p.Longitude + 180.0) / 360.0;
                double sinLat = Math.Sin(p.Latitude * Math.PI / 180.0);
                sinLat = Math.Min(Math.Max(sinLat, -0.9999), 0.9999);
                double y = 0.5 - Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI);

                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }


            double spanX = Math.Max(1e-9, maxX - minX);
            double spanY = Math.Max(1e-9, maxY - minY);


            double zoomX = Math.Log(width / (tileSize * spanX), 2);
            double zoomY = Math.Log(height / (tileSize * spanY), 2);

  
            double continuousZoom = Math.Min(zoomX, zoomY);
            int chosenZoom = (int)Math.Floor(continuousZoom);
            if (double.IsInfinity(continuousZoom) || double.IsNaN(continuousZoom))
                chosenZoom = maxZoom;

            chosenZoom = Math.Max(minZoom, Math.Min(maxZoom, chosenZoom));

  
            if (maxX - minX < 1e-6 && maxY - minY < 1e-6)
            {
                chosenZoom = Math.Min(maxZoom, chosenZoom + 2);
            }


            double centerXNorm = (minX + maxX) / 2.0;
            double centerYNorm = (minY + maxY) / 2.0;

            double centerLon = centerXNorm * 360.0 - 180.0;
            double centerLat = InverseMercatorLat(centerYNorm);

            await LoadMap(centerLat, centerLon, chosenZoom, properties);
        }

        private static double InverseMercatorLat(double y)
        {
            double n = Math.PI * (1.0 - 2.0 * y);
            double latRad = Math.Atan(Math.Sinh(n));
            return latRad * 180.0 / Math.PI;
        }

        private async Task LoadMap(double latitude, double longitude, int zoom = 15, IEnumerable<Property>? markers = null)
        {
            try
            {
                lblStatus.Text = "Loading map...";
                btnLoadMap.Enabled = false;

                if (pictureBoxMap.Image != null)
                {
                    pictureBoxMap.Image.Dispose();
                    pictureBoxMap.Image = null;
                }

                int width = pictureBoxMap.Width;
                int height = pictureBoxMap.Height;
                Image mapImage = await GenerateStaticMap(latitude, longitude, zoom, width, height, markers);
                pictureBoxMap.Image = mapImage;

                lblStatus.Text = $"Map loaded: {latitude}, {longitude} (markers: {markerHitboxes.Count})";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Error loading map: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLoadMap.Enabled = true;
            }
        }

        private async Task<Image> GenerateStaticMap(double latitude, double longitude,
            int zoom, int width, int height, IEnumerable<Property>? markers = null)
        {

            markerHitboxes.Clear();

            Bitmap mapImage = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(mapImage))
            {
                g.Clear(Color.LightGray);

                var (centerPx, centerPy) = LatLonToWorldPixels(latitude, longitude, zoom);

                const int tileSize = 256;
                double topLeftWorldPx = centerPx - width / 2.0;
                double topLeftWorldPy = centerPy - height / 2.0;

                int firstTileX = (int)Math.Floor(topLeftWorldPx / tileSize);
                int firstTileY = (int)Math.Floor(topLeftWorldPy / tileSize);
                int lastTileX = (int)Math.Floor((topLeftWorldPx + width) / tileSize);
                int lastTileY = (int)Math.Floor((topLeftWorldPy + height) / tileSize);

                int maxTile = (int)Math.Pow(2, zoom);

                for (int tx = firstTileX; tx <= lastTileX; tx++)
                {
                    for (int ty = firstTileY; ty <= lastTileY; ty++)
                    {
                        int wrappedTx = tx;
                        if (wrappedTx < 0) wrappedTx = (wrappedTx % maxTile + maxTile) % maxTile;
                        if (wrappedTx >= maxTile) wrappedTx = wrappedTx % maxTile;
                        if (ty < 0 || ty >= maxTile) continue;

                        string tileUrl = $"https://tile.openstreetmap.org/{zoom}/{wrappedTx}/{ty}.png";

                        try
                        {
                            byte[] tileData = await httpClient.GetByteArrayAsync(tileUrl);
                            using (var ms = new MemoryStream(tileData))
                            using (Image tileImage = Image.FromStream(ms))
                            {
                                double tileWorldPx = tx * tileSize;
                                double tileWorldPy = ty * tileSize;

                                int drawX = (int)Math.Round(tileWorldPx - topLeftWorldPx);
                                int drawY = (int)Math.Round(tileWorldPy - topLeftWorldPy);

                                g.DrawImage(tileImage, drawX, drawY, tileSize, tileSize);
                            }
                        }
                        catch
                        {
                            double tileWorldPx = tx * tileSize;
                            double tileWorldPy = ty * tileSize;
                            int drawX = (int)Math.Round(tileWorldPx - topLeftWorldPx);
                            int drawY = (int)Math.Round(tileWorldPy - topLeftWorldPy);
                            g.FillRectangle(Brushes.Gray, drawX, drawY, tileSize, tileSize);
                        }
                    }
                }

                var markersToDraw = (markers != null) ? markers.ToList() : GetSelectedAgencyProperties().ToList();

 
                foreach (var prop in markersToDraw)
                {
                    var (propPx, propPy) = LatLonToWorldPixels(prop.Latitude, prop.Longitude, zoom);

                    int imgX = (int)Math.Round(propPx - topLeftWorldPx);
                    int imgY = (int)Math.Round(propPy - topLeftWorldPy);

                    const int hitSize = 20;
                    Rectangle hitRect = new Rectangle(imgX - hitSize / 2, imgY - hitSize / 2, hitSize, hitSize);
                    if (hitRect.Right < 0 || hitRect.Left > width || hitRect.Bottom < 0 || hitRect.Top > height)
                        continue;

                    DrawPropertyMarker(g, imgX, imgY, prop);

                    var clamped = Rectangle.Intersect(new Rectangle(0, 0, width, height), hitRect);
                    if (!clamped.IsEmpty)
                    {
                        if (!markerHitboxes.TryGetValue(clamped, out var list))
                        {
                            list = new List<Property>();
                            markerHitboxes[clamped] = list;
                        }

                        // avoid duplicates
                        if (!list.Contains(prop))
                            list.Add(prop);
                    }
                }
            }

            return mapImage;
        }

        private (double px, double py) LatLonToWorldPixels(double lat, double lon, int zoom)
        {
            const int tileSize = 256;
            double mapSize = tileSize * Math.Pow(2, zoom);

            double x = (lon + 180.0) / 360.0 * mapSize;

            double sinLat = Math.Sin(lat * Math.PI / 180.0);
            sinLat = Math.Min(Math.Max(sinLat, -0.9999), 0.9999);
            double y = (0.5 - Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI)) * mapSize;

            return (x, y);
        }


        private void DrawPropertyMarker(Graphics g, int x, int y, Property prop)
        {
            int r = 8;

            using (Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                g.FillEllipse(shadow, x - r + 2, y - r + 2, r * 2, r * 2);
            }

            Color fillColor;
            Color borderColor;

            if (prop is IRentable rentable)
            {
                if (rentable.IsRented)
                {
                    // unavailable / rented
                    fillColor = Color.Gray;
                    borderColor = Color.DarkSlateGray;
                }
                else
                {
                    // available to rent
                    fillColor = Color.LimeGreen;
                    borderColor = Color.DarkGreen;
                }
            }
            else
            {
                // not for rent
                fillColor = Color.DodgerBlue;
                borderColor = Color.Navy;
            }

            using (Brush fill = new SolidBrush(fillColor))
            using (Pen border = new Pen(borderColor, 2))
            {
                g.FillEllipse(fill, x - r, y - r, r * 2, r * 2);
                g.DrawEllipse(border, x - r, y - r, r * 2, r * 2);
            }
        }

        private async void PictureBoxMap_MouseClick(object? sender, MouseEventArgs e)
        {
            if (pictureBoxMap.Image == null) return;

            Point click = e.Location;

            // collect all properties whose hit-rect contains the click
            var directMatches = new List<Property>();
            foreach (var kvp in markerHitboxes)
            {
                if (kvp.Key.Contains(click))
                {
                    directMatches.AddRange(kvp.Value);
                }
            }

            var uniqueMatches = directMatches.Distinct().ToList();

            if (uniqueMatches.Count == 1)
            {
                await ShowPropertyInfo(uniqueMatches[0]);
                return;
            }
            else if (uniqueMatches.Count > 1)
            {
                using var chooser = new PropertyChooserForm(uniqueMatches);
                if (chooser.ShowDialog(this) == DialogResult.OK && chooser.SelectedProperty != null)
                {
                    await ShowPropertyInfo(chooser.SelectedProperty);
                }
                return;
            }

        }

        private static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private async Task ShowPropertyInfo(Property prop)
        {
            if (prop == null) return;

            string text = prop.ToString() + Environment.NewLine
                          + $"Lat: {prop.Latitude}, Lon: {prop.Longitude}";

            if (prop is IRentable rentable)
            {
                if (rentable.IsRented)
                {
                    MessageBox.Show(text + Environment.NewLine + "Status: Rented", "Property Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var result = MessageBox.Show(text + Environment.NewLine + "Status: Available for rent" + Environment.NewLine + "Do you want to rent this property?", "Property Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        rentable.IsRented = true;

  
                        try
                        {
                            await PersistenceService.SaveAsync(agencies);
                        }
                        catch
                        {
                            // ignore saving errors for now
                        }


                        RefreshProperties();

                        var props = GetSelectedAgencyProperties().ToList();
                        if (props.Any())
                        {
                            ShowAgencyOnMap(props);
                        }
                        else
                        {
                            // fallback: center on the rented property
                            _ = ShowSinglePropertyOnMap(prop);
                        }

                        MessageBox.Show("Property has been marked as rented.", "Rented", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show(text + Environment.NewLine + "Not for rent.", "Property Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}