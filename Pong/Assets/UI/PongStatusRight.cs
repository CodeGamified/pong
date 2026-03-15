// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeGamified.TUI;
using CodeGamified.Time;
using CodeGamified.Settings;
using CodeGamified.Quality;

namespace Pong.UI
{
    /// <summary>
    /// Right status panel — three-column: CONTROLS │ QUALITY/FONT │ AUDIO.
    /// Left-edge dragger is linked to the AI code debugger above.
    /// </summary>
    public class PongStatusRight : TerminalWindow
    {
        private bool IsExpanded => totalRows > 3;
        private bool _panelsReady;
        private int _col2Start;
        private int _col3Start;

        // Audio slider overlays
        private Slider _masterSlider;
        private Slider _musicSlider;
        private Slider _sfxSlider;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "CONTROLS";
            totalRows = 40;
        }

        protected override void OnLayoutReady()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null || rows.Count == 0) return;
            float h = rt.rect.height;
            float rowH = rows[0].RowHeight;
            if (rowH <= 0) return;
            int fitRows = Mathf.Max(2, Mathf.FloorToInt(h / rowH));
            if (fitRows != totalRows)
            {
                for (int i = 0; i < rows.Count; i++)
                    rows[i].gameObject.SetActive(i < fitRows);
                totalRows = fitRows;
            }
            SetupThreePanels();
        }

        private void SetupThreePanels()
        {
            _col2Start = totalChars / 3;
            _col3Start = (totalChars * 2) / 3;
            foreach (var row in rows)
                row.SetThreePanelMode(true, _col2Start, _col3Start);
            _panelsReady = true;
            CreateAudioSliders();
        }

        private void CreateAudioSliders()
        {
            // Audio bars render at content rows 1-3 → actual rows 3,4,5
            // Bar text: "  Mst [██████████] 100%" — bar starts at char 6, width 12
            const int barOffset = 6;
            const int barWidth = 12;
            int sliderStart = _col3Start + barOffset;

            if (rows.Count <= 5) return;

            // Reposition existing sliders (layout may change after initial creation)
            if (_masterSlider != null)
            {
                rows[3].RepositionSliderOverlay(sliderStart, barWidth);
                rows[4].RepositionSliderOverlay(sliderStart, barWidth);
                rows[5].RepositionSliderOverlay(sliderStart, barWidth);
                return;
            }

            _masterSlider = rows[3].CreateSliderOverlay(sliderStart, barWidth);
            _masterSlider.SetValueWithoutNotify(SettingsBridge.MasterVolume);
            _masterSlider.onValueChanged.AddListener(v => SettingsBridge.SetMasterVolume(v));

            _musicSlider = rows[4].CreateSliderOverlay(sliderStart, barWidth);
            _musicSlider.SetValueWithoutNotify(SettingsBridge.MusicVolume);
            _musicSlider.onValueChanged.AddListener(v => SettingsBridge.SetMusicVolume(v));

            _sfxSlider = rows[5].CreateSliderOverlay(sliderStart, barWidth);
            _sfxSlider.SetValueWithoutNotify(SettingsBridge.SfxVolume);
            _sfxSlider.onValueChanged.AddListener(v => SettingsBridge.SetSfxVolume(v));
        }

        protected override void Update()
        {
            base.Update();
            if (!rowsReady || !IsExpanded) return;
            HandleInput();
        }

        private void HandleInput()
        {
            // Quality cycle
            if (Input.GetKeyDown(KeyCode.Q))
            {
                int next = (SettingsBridge.QualityLevel + 1) % 4;
                SettingsBridge.SetQualityLevel(next);
                Debug.Log($"[Menu] Quality → {(QualityTier)next}");
            }

            // Font size: [ / ] keys
            if (Input.GetKeyDown(KeyCode.RightBracket))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize + 1f);
            if (Input.GetKeyDown(KeyCode.LeftBracket))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize - 1f);

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Audio: F5/F6/F7 cycle Master/Music/SFX by ±10%
            if (Input.GetKeyDown(KeyCode.F5))
                SettingsBridge.SetMasterVolume(SettingsBridge.MasterVolume + (shift ? -0.1f : 0.1f));
            if (Input.GetKeyDown(KeyCode.F6))
                SettingsBridge.SetMusicVolume(SettingsBridge.MusicVolume + (shift ? -0.1f : 0.1f));
            if (Input.GetKeyDown(KeyCode.F7))
                SettingsBridge.SetSfxVolume(SettingsBridge.SfxVolume + (shift ? -0.1f : 0.1f));
        }

        private void Set3(int r, string p1, string p2, string p3)
        {
            Row(r)?.SetThreePanelTexts(p1 ?? "", p2 ?? "", p3 ?? "");
        }

        protected override void Render()
        {
            ClearAllRows();

            if (!_panelsReady)
            {
                SetRow(0, BuildCollapsedFlat());
                return;
            }

            // Row 0 — collapsed header across 3 columns
            Set3(0, BuildCollapsedCol1(), BuildCollapsedCol2(), BuildCollapsedCol3());

            if (!IsExpanded) return;

            // Row 1 — separator per column
            string sep1 = Separator(_col2Start - 5);
            string sep2 = Separator(_col3Start - _col2Start - 5);
            string sep3 = Separator(totalChars - _col3Start - 5);
            Set3(1, sep1, sep2, sep3);

            // Sync slider positions with current volumes (e.g. changed via keyboard)
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(SettingsBridge.MasterVolume);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(SettingsBridge.MusicVolume);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(SettingsBridge.SfxVolume);

            // Rows 2+ — three-column content
            var col1 = BuildControlsColumn();
            var col2 = BuildQualityFontColumn();
            var col3 = BuildAudioColumn();

            int maxLines = Mathf.Max(col1.Length, Mathf.Max(col2.Length, col3.Length));
            for (int i = 0; i < maxLines; i++)
            {
                int r = i + 2;
                if (r >= totalRows) break;
                Set3(r,
                    i < col1.Length ? col1[i] : "",
                    i < col2.Length ? col2[i] : "",
                    i < col3.Length ? col3[i] : "");
            }
        }

        private string BuildCollapsedFlat()
        {
            var sim = SimulationTime.Instance;
            if (sim == null) return "";
            return $" {sim.GetFormattedTimeScale()} {TUIGlyphs.BoxV} +/- speed {TUIGlyphs.BoxV} SPACE pause";
        }

        private string BuildCollapsedCol1()
        {
            var sim = SimulationTime.Instance;
            string speed = sim != null ? sim.GetFormattedTimeScale() : "1x";
            string paused = (sim != null && sim.isPaused)
                ? TUIColors.Fg(TUIColors.BrightYellow, " PAUSED")
                : "";
            return $" {speed}{paused}";
        }

        private string BuildCollapsedCol2()
        {
            string tierName = ((QualityTier)SettingsBridge.QualityLevel).ToString();
            return $" {TUIColors.Fg(TUIColors.BrightGreen, tierName)}";
        }

        private string BuildCollapsedCol3()
        {
            return $" Vol:{SettingsBridge.MasterVolume * 100:F0}%";
        }

        // ── Column 1: CONTROLS ───────────────────────────────────

        private string[] BuildControlsColumn()
        {
            var lines = new List<string>();
            var sim = SimulationTime.Instance;
            string speed = sim != null ? sim.GetFormattedTimeScale() : "1x";
            string paused = (sim != null && sim.isPaused)
                ? TUIColors.Fg(TUIColors.BrightYellow, " PAUSED")
                : "";

            lines.Add($" {TUIColors.Bold("CONTROLS")}{paused}");
            lines.Add($"  Speed: {TUIColors.Fg(TUIColors.BrightGreen, speed)}");
            lines.Add("");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[1-4]")}   AI diff");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[S+1-4]")} Sample");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[R]")}     Reset");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[+/-]")}   Speed");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[SPACE]")} Pause");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[W]")}     Warp");
            return lines.ToArray();
        }

        // ── Column 2: QUALITY / FONT ─────────────────────────────

        private string[] BuildQualityFontColumn()
        {
            var lines = new List<string>();

            string tierName = ((QualityTier)SettingsBridge.QualityLevel).ToString();
            lines.Add($" {TUIColors.Bold("QUALITY")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, tierName)}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[Q]")} Cycle");
            lines.Add("");
            lines.Add($" {TUIColors.Bold("DISPLAY")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, $"{SettingsBridge.FontSize:F0}pt")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[]]")} Font+");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[[]")} Font-");
            return lines.ToArray();
        }

        // ── Column 3: AUDIO ──────────────────────────────────────

        private string[] BuildAudioColumn()
        {
            var lines = new List<string>();

            lines.Add($" {TUIColors.Bold("AUDIO")}");
            lines.Add($"  {VolumeBar("Mst", SettingsBridge.MasterVolume)}");
            lines.Add($"  {VolumeBar("Mus", SettingsBridge.MusicVolume)}");
            lines.Add($"  {VolumeBar("SFX", SettingsBridge.SfxVolume)}");
            lines.Add("");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F5]")} Mst");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F6]")} Mus");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[F7]")} SFX");
            lines.Add($"  {TUIColors.Dimmed("S+key = -")}");
            return lines.ToArray();
        }

        private static string VolumeBar(string label, float vol)
        {
            int filled = Mathf.RoundToInt(vol * 10);
            string bar = new string('█', filled) + new string('░', 10 - filled);
            string pct = $"{vol * 100:F0}%";
            return $"{label} [{TUIColors.Fg(TUIColors.BrightGreen, bar)}] {TUIColors.Dimmed(pct)}";
        }
    }
}
