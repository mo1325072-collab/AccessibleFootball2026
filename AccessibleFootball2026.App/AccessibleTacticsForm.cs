using System.Drawing;
using System.Windows.Forms;
using AccessibleFootball2026.Audio;
using AccessibleFootball2026.Core.Models;

namespace AccessibleFootball2026.App;

/// <summary>Native Windows controls, with changes committed only by Apply.</summary>
public sealed class AccessibleTacticsForm : Form
{
    public AccessibleTacticsForm(Team team, IScreenReader reader)
    {
        string T(string ar, string en) => LocalizationService.Text(ar, en);
        Text = T("الأسلوب والتكتيك", "Playing style and tactics") + ": " + team.Name;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(680, 510);
        MinimumSize = new Size(580, 480);
        Font = new Font("Segoe UI", 11);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 2, RowCount = 7, AutoScroll = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        for (int i = 0; i < 7; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        void Add(string name, Control control, int row)
        {
            panel.Controls.Add(new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true }, 0, row);
            control.AccessibleName = name;
            control.Dock = DockStyle.Fill;
            control.TabIndex = row;
            panel.Controls.Add(control, 1, row);
        }
        TrackBar Slider(int value) => new() { Minimum = 0, Maximum = 100, SmallChange = 1, LargeChange = 10, TickFrequency = 10, Value = value };
        ComboBox Choice(string[] options, int index)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            c.Items.AddRange(options);
            c.SelectedIndex = Math.Clamp(index, 0, options.Length - 1);
            return c;
        }
        var pressing = Slider(team.Tactics.PressingIntensity);
        var passing = Slider(team.Tactics.PassingDirectness);
        var tackling = Choice(new[] { T("حذر", "Cautious"), T("عادي", "Normal"), T("متوسط", "Moderate"), T("حماسي", "Aggressive"), T("متهور", "Reckless") }, (int)team.Tactics.Tackling);
        var focusValues = new[] { AttackingFocus.Central, AttackingFocus.ShootOnSight, AttackingFocus.CounterAttack, AttackingFocus.Wings, AttackingFocus.LongBall };
        var focus = Choice(new[] { T("التمريرات", "Passing"), T("التسديد عن بعد", "Long-range shooting"), T("الهجوم المرتد", "Counter-attack"), T("الأجناب", "Wings"), T("الكرات الطويلة", "Long balls") }, Array.IndexOf(focusValues, team.Tactics.Focus));
        var marking = Choice(new[] { T("دفاع منطقة", "Zonal marking"), T("دفاع رجل لرجل", "Man-to-man marking") }, (int)team.Tactics.Marking);
        var offside = new CheckBox { Checked = team.Tactics.OffsideTrap, Text = T("مصيدة التسلل", "Offside trap"), AutoSize = true };
        Add(T("شريط الضغط", "Pressing"), pressing, 0);
        Add(T("شريط التمرير", "Passing"), passing, 1);
        Add(T("أسلوب العرقلة", "Tackling"), tackling, 2);
        Add(T("أسلوب اللعب", "Playing style"), focus, 3);
        Add(T("طريقة الدفاع", "Defensive marking"), marking, 4);
        Add(T("مصيدة التسلل", "Offside trap"), offside, 5);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, TabIndex = 6 };
        var apply = new Button { Text = T("اعتماد", "Apply"), AutoSize = true, MinimumSize = new Size(110, 36) };
        var cancel = new Button { Text = T("إلغاء", "Cancel"), DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(110, 36) };
        apply.Click += (_, _) =>
        {
            team.Tactics.PressingIntensity = pressing.Value;
            team.Tactics.PassingDirectness = passing.Value;
            team.Tactics.UseLinkedDefensiveLine();
            team.Tactics.UseLinkedTempo();
            team.Tactics.Tackling = (TacklingAggression)tackling.SelectedIndex;
            team.Tactics.Focus = focusValues[focus.SelectedIndex];
            team.Tactics.Marking = (DefensiveMarking)marking.SelectedIndex;
            team.Tactics.OffsideTrap = offside.Checked;
            reader.Speak(T("تم اعتماد التكتيك.", "Tactics applied."));
            DialogResult = DialogResult.OK;
            Close();
        };
        buttons.Controls.Add(apply);
        buttons.Controls.Add(cancel);
        panel.Controls.Add(buttons, 0, 6);
        panel.SetColumnSpan(buttons, 2);
        Controls.Add(panel);
        AcceptButton = apply;
        CancelButton = cancel;
        Shown += (_, _) => pressing.Focus();
    }
}
