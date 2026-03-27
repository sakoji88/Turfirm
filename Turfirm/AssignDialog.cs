using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using Turfirm.Infrastructure;

namespace Turfirm
{
    public class AssignDialog : Form
    {
        private readonly ComboBox _cbGuides = new ComboBox();
        private readonly ComboBox _cbTransports = new ComboBox();

        public int SelectedGuideId => Convert.ToInt32(_cbGuides.SelectedValue);
        public int SelectedTransportId => Convert.ToInt32(_cbTransports.SelectedValue);

        public AssignDialog()
        {
            Text = "Назначение ресурсов";
            Width = 400;
            Height = 220;
            StartPosition = FormStartPosition.CenterParent;
            Icon = System.Drawing.SystemIcons.Warning;

            Controls.Add(new Label { Text = "Гид", Left = 30, Top = 35, Width = 120 });
            Controls.Add(new Label { Text = "Транспорт", Left = 30, Top = 80, Width = 120 });

            _cbGuides.Left = 150;
            _cbGuides.Top = 30;
            _cbGuides.Width = 200;
            _cbTransports.Left = 150;
            _cbTransports.Top = 75;
            _cbTransports.Width = 200;

            Controls.Add(_cbGuides);
            Controls.Add(_cbTransports);

            var btnOk = new Button { Text = "Подтвердить", Left = 150, Top = 130, Width = 120, DialogResult = DialogResult.OK };
            Controls.Add(btnOk);
            AcceptButton = btnOk;

            LoadData();
        }

        private void LoadData()
        {
            using (var con = Db.Open(Db.AppConnection))
            {
                using (var gAdapter = new SqlDataAdapter("SELECT Id, FullName FROM Guides WHERE IsActive=1", con))
                {
                    var dt = new DataTable();
                    gAdapter.Fill(dt);
                    _cbGuides.DataSource = dt;
                    _cbGuides.DisplayMember = "FullName";
                    _cbGuides.ValueMember = "Id";
                }

                using (var tAdapter = new SqlDataAdapter("SELECT Id, Name + ' (' + CAST(Capacity AS NVARCHAR(10)) + ')' AS Name FROM Transports WHERE IsActive=1", con))
                {
                    var dt = new DataTable();
                    tAdapter.Fill(dt);
                    _cbTransports.DataSource = dt;
                    _cbTransports.DisplayMember = "Name";
                    _cbTransports.ValueMember = "Id";
                }
            }
        }
    }
}
