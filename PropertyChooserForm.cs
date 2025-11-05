
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RealEstateMap
{
    // Simple modal dialog to pick one property from a short list
    public class PropertyChooserForm : Form
    {
        private readonly ListBox listBox;
        private readonly Button btnOk;
        private readonly Button btnCancel;

        public Property SelectedProperty { get; private set; }

        public PropertyChooserForm(IEnumerable<Property> properties)
        {
            Text = "Choose property";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(420, 320);

            listBox = new ListBox
            {
                Location = new Point(10, 10),
                Size = new Size(380, 220),
                DisplayMember = "Address"
            };

            // Add properties (keep original objects)
            listBox.Items.AddRange(properties is Property[] arr ? arr : new List<Property>(properties).ToArray());
            listBox.DoubleClick += ListBox_DoubleClick;
            Controls.Add(listBox);

            btnOk = new Button { Location = new Point(10, 240), Size = new Size(120, 30), Text = "OK" };
            btnOk.Click += BtnOk_Click;
            Controls.Add(btnOk);

            btnCancel = new Button { Location = new Point(140, 240), Size = new Size(120, 30), Text = "Cancel" };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(btnCancel);
        }

        private void ListBox_DoubleClick(object? sender, EventArgs e)
        {
            CommitSelection();
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            CommitSelection();
        }

        private void CommitSelection()
        {
            if (listBox.SelectedItem is Property p)
            {
                SelectedProperty = p;
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Select a property from the list.", "Select", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}