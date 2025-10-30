
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace RealEstateMap
{
    public class AddPropertyForm : Form
    {
        public Property CreatedProperty { get; private set; }

        private ComboBox cmbType;
        private TextBox txtAddress;
        private TextBox txtIndoorArea;
        private TextBox txtPropertyValue;
        private TextBox txtLatitude;
        private TextBox txtLongitude;

   
        private TextBox txtFloor;
        private CheckBox chkElevator;

        private TextBox txtOutdoorArea;

 
        private TextBox txtMonthlyRent;

        private Button btnOk;
        private Button btnCancel;

        public AddPropertyForm()
        {
            Initialize();
        }

        private void Initialize()
        {
            Text = "Add Property";
            Size = new Size(420, 420);
            StartPosition = FormStartPosition.CenterParent;

            var lblType = new Label { Location = new Point(10, 10), Text = "Type", Width = 100 };
            cmbType = new ComboBox { Location = new Point(120, 10), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new object[] { "Apartment", "RentableApartment", "House" });
            cmbType.SelectedIndex = 0;
            cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;

            var lblAddress = new Label { Location = new Point(10, 50), Text = "Address", Width = 100 };
            txtAddress = new TextBox { Location = new Point(120, 50), Width = 250 };

            var lblIndoor = new Label { Location = new Point(10, 90), Text = "Indoor Area (m²)", Width = 100 };
            txtIndoorArea = new TextBox { Location = new Point(120, 90), Width = 250 };

            var lblPropertyValue = new Label { Location = new Point(10, 130), Text = "Value", Width = 100 };
            txtPropertyValue = new TextBox { Location = new Point(120, 130), Width = 250 };

            var lblLatitude = new Label { Location = new Point(10, 170), Text = "Latitude", Width = 100 };
            txtLatitude = new TextBox { Location = new Point(120, 170), Width = 250 };

            var lblLongitude = new Label { Location = new Point(10, 210), Text = "Longitude", Width = 100 };
            txtLongitude = new TextBox { Location = new Point(120, 210), Width = 250 };

            // Apartment controls
            var lblFloor = new Label { Location = new Point(10, 250), Text = "Floor", Width = 100 };
            txtFloor = new TextBox { Location = new Point(120, 250), Width = 100 };

            chkElevator = new CheckBox { Location = new Point(230, 250), Text = "Has Elevator" };

            // Rentable controls
            var lblMonthly = new Label { Location = new Point(10, 290), Text = "Monthly Rent", Width = 100 };
            txtMonthlyRent = new TextBox { Location = new Point(120, 290), Width = 250 };

            // House controls
            var lblOutdoor = new Label { Location = new Point(10, 330), Text = "Outdoor Area", Width = 100 };
            txtOutdoorArea = new TextBox { Location = new Point(120, 330), Width = 250 };

            btnOk = new Button { Location = new Point(120, 360), Text = "OK", Width = 100 };
            btnCancel = new Button { Location = new Point(240, 360), Text = "Cancel", Width = 100 };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] {
                lblType, cmbType, lblAddress, txtAddress, lblIndoor, txtIndoorArea,
                lblPropertyValue, txtPropertyValue, lblLatitude, txtLatitude, lblLongitude, txtLongitude,
                lblFloor, txtFloor, chkElevator, lblMonthly, txtMonthlyRent, lblOutdoor, txtOutdoorArea,
                btnOk, btnCancel
            });

            UpdateFieldVisibility();
        }

        private void CmbType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateFieldVisibility();
        }

        private void UpdateFieldVisibility()
        {
            string type = cmbType.SelectedItem!.ToString();
            bool isApartment = type == "Apartment";
            bool isRentable = type == "RentableApartment";
            bool isHouse = type == "House";

            txtFloor.Visible = isApartment || isRentable;
            chkElevator.Visible = isApartment || isRentable;
            txtMonthlyRent.Visible = isRentable;
            txtOutdoorArea.Visible = isHouse;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            try
            {
                string address = txtAddress.Text.Trim();
                if (string.IsNullOrEmpty(address)) throw new Exception("Address is required.");

                double indoor = ParseDouble(txtIndoorArea.Text, "Indoor area");
                double value = ParseDouble(txtPropertyValue.Text, "Property value");
                double lat = ParseDouble(txtLatitude.Text, "Latitude");
                double lon = ParseDouble(txtLongitude.Text, "Longitude");

                string type = cmbType.SelectedItem!.ToString();

                if (type == "Apartment")
                {
                    int floor = (int)ParseDouble(txtFloor.Text, "Floor");
                    bool elevator = chkElevator.Checked;
                    CreatedProperty = new Apartment(address, indoor, floor, elevator, value, lat, lon);
                }
                else if (type == "RentableApartment")
                {
                    int floor = (int)ParseDouble(txtFloor.Text, "Floor");
                    bool elevator = chkElevator.Checked;
                    double monthly = ParseDouble(txtMonthlyRent.Text, "Monthly rent");
                    CreatedProperty = new RentableApartment(address, indoor, floor, elevator, monthly, value, lat, lon);
                }
                else // House
                {
                    double outdoor = ParseDouble(txtOutdoorArea.Text, "Outdoor area");
                    CreatedProperty = new House(address, indoor, outdoor, value, lat, lon);
                }

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private double ParseDouble(string text, string name)
        {
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                return d;

            throw new Exception($"{name} is invalid.");
        }
    }
}