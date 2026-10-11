using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.Save;
using UnityEngine;

namespace NihongoLife.Notebook
{
    [Serializable]
    public sealed class NotebookPage
    {
        public string title = string.Empty;
        public string body = string.Empty;
        public long updatedUnix;
        public bool IsBlank => string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body);
    }

    /// <summary>The player's paper notebook (saved with the progress). The number of sheets is limited: a blank
    /// notebook starts with <see cref="NotebookService.StartPages"/>; more paper is bought at Hibari Mart.</summary>
    [Serializable]
    public sealed class NotebookRecord
    {
        public int pages = NotebookService.StartPages;
        public int currentPage;
        public List<NotebookPage> entries = new List<NotebookPage>();
    }

    /// <summary>
    /// Owns the notebook data: page count, page contents and saving. Every page holds at most
    /// <see cref="PageChars"/> characters — a written-on page stays used until the player erases it, so paper is a
    /// real resource. The UI (<see cref="NihongoLife.UI.NotebookUI"/>) and the exam "copy highlights" button both
    /// write through here.
    /// </summary>
    public static class NotebookService
    {
        public const int StartPages = 6;
        public const int MaxPages = 99;
        public const int PageChars = 600;
        public const int TitleChars = 40;

        private static NotebookRecord _fallback;

        public static event Action Changed;

        public static NotebookRecord Record
        {
            get
            {
                NotebookRecord record = null;
                if (GameServices.TryGet(out IProgressRepository repository))
                {
                    var progress = repository.GetProgress();
                    if (progress != null)
                    {
                        if (progress.notebook == null) progress.notebook = new NotebookRecord();
                        record = progress.notebook;
                    }
                }
                record ??= _fallback ??= new NotebookRecord();
                Normalise(record);
                return record;
            }
        }

        public static int Pages => Record.pages;
        public static int UsedPages
        {
            get
            {
                int used = 0;
                foreach (var page in Record.entries) if (!page.IsBlank) used++;
                return used;
            }
        }
        public static int CurrentPage => Record.currentPage;

        public static NotebookPage Page(int index)
        {
            var record = Record;
            return record.entries[Mathf.Clamp(index, 0, record.pages - 1)];
        }

        public static void SetCurrentPage(int index)
        {
            var record = Record;
            int clamped = Mathf.Clamp(index, 0, record.pages - 1);
            if (clamped == record.currentPage) return;
            record.currentPage = clamped;
            Changed?.Invoke();
        }

        /// <summary>Writes a page (clipped to the page size). Saving is deferred to <see cref="Save"/>.</summary>
        public static void Write(int index, string title, string body)
        {
            var page = Page(index);
            title = Clip(title, TitleChars);
            body = Clip(body, PageChars);
            if (page.title == title && page.body == body) return;
            page.title = title;
            page.body = body;
            page.updatedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Changed?.Invoke();
        }

        public static void Erase(int index)
        {
            var page = Page(index);
            if (page.IsBlank) return;
            page.title = string.Empty;
            page.body = string.Empty;
            page.updatedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Changed?.Invoke();
            Save();
        }

        /// <summary>Appends text to the current page; when it does not fit, continues on the next blank pages.
        /// Returns how many characters could not be written (0 = everything fitted).</summary>
        public static int Append(string text, string titleIfBlank = null)
        {
            var record = Record;
            string rest = (text ?? string.Empty).Trim();
            int index = record.currentPage;
            while (rest.Length > 0 && index < record.pages)
            {
                var page = record.entries[index];
                if (index != record.currentPage && !page.IsBlank) { index++; continue; }
                string separator = page.body.Length > 0 ? "\n" : string.Empty;
                int room = PageChars - page.body.Length - separator.Length;
                if (room > 8)
                {
                    string piece = rest.Length <= room ? rest : rest.Substring(0, room);
                    if (page.IsBlank && !string.IsNullOrEmpty(titleIfBlank)) page.title = Clip(titleIfBlank, TitleChars);
                    page.body += separator + piece;
                    page.updatedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    rest = rest.Substring(piece.Length).TrimStart();
                    record.currentPage = index;
                }
                index++;
            }
            Changed?.Invoke();
            Save();
            return rest.Length;
        }

        /// <summary>Adds bought sheets; returns how many were actually added (the binder holds <see cref="MaxPages"/>).</summary>
        public static int AddPages(int count)
        {
            var record = Record;
            int added = Mathf.Clamp(count, 0, MaxPages - record.pages);
            if (added <= 0) return 0;
            record.pages += added;
            Normalise(record);
            Changed?.Invoke();
            Save();
            return added;
        }

        public static void Save()
        {
            if (!GameServices.TryGet(out IProgressRepository repository)) return;
            var progress = repository.GetProgress();
            if (progress == null) return;
            progress.notebook ??= Record;
            repository.SaveProgress(progress);
        }

        private static void Normalise(NotebookRecord record)
        {
            record.pages = Mathf.Clamp(record.pages <= 0 ? StartPages : record.pages, 1, MaxPages);
            record.entries ??= new List<NotebookPage>();
            while (record.entries.Count < record.pages) record.entries.Add(new NotebookPage());
            for (int i = 0; i < record.entries.Count; i++) record.entries[i] ??= new NotebookPage();
            record.currentPage = Mathf.Clamp(record.currentPage, 0, record.pages - 1);
        }

        private static string Clip(string value, int max)
        {
            value ??= string.Empty;
            return value.Length <= max ? value : value.Substring(0, max);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _fallback = null;
            Changed = null;
        }
    }
}
