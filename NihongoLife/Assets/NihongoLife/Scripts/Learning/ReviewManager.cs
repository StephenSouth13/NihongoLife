using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Save;

namespace NihongoLife.Learning
{
    public interface IReviewManager : IGameService
    {
        void RecordIncorrectAnswer(string questionId, string targetId);
        void RecordCorrectAnswer(string questionId);
        List<ReviewRecord> GetPendingReviews();
    }

    public class ReviewManager : MonoBehaviour, IReviewManager
    {
        public bool IsInitialized { get; private set; }
        private IProgressRepository _progressRepository;

        public void Initialize()
        {
            _progressRepository = ServiceLocator.Get<IProgressRepository>();
            IsInitialized = true;
        }

        public void RecordIncorrectAnswer(string questionId, string targetId)
        {
            if (!IsInitialized || _progressRepository == null) return;

            var progress = _progressRepository.GetProgress();
            if (progress.reviewItems == null)
            {
                progress.reviewItems = new List<ReviewRecord>();
            }

            var record = progress.reviewItems.FirstOrDefault(r => r.questionId == questionId);
            if (record != null)
            {
                record.incorrectCount++;
                // Schedule next review for tomorrow (simple SRS logic)
                record.nextReviewAt = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds();
            }
            else
            {
                progress.reviewItems.Add(new ReviewRecord
                {
                    questionId = questionId,
                    targetId = targetId,
                    incorrectCount = 1,
                    nextReviewAt = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()
                });
            }

            _progressRepository.SaveProgress(progress);
        }

        public void RecordCorrectAnswer(string questionId)
        {
            if (!IsInitialized || _progressRepository == null) return;

            var progress = _progressRepository.GetProgress();
            if (progress.reviewItems == null) return;

            var record = progress.reviewItems.FirstOrDefault(r => r.questionId == questionId);
            if (record != null)
            {
                // Simple logic: remove from review once answered correctly, 
                // or we could decrease incorrectCount and increase nextReviewAt.
                progress.reviewItems.Remove(record);
                _progressRepository.SaveProgress(progress);
            }
        }

        public List<ReviewRecord> GetPendingReviews()
        {
            if (!IsInitialized || _progressRepository == null) return new List<ReviewRecord>();

            var progress = _progressRepository.GetProgress();
            if (progress.reviewItems == null) return new List<ReviewRecord>();

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return progress.reviewItems.Where(r => r.nextReviewAt <= now).ToList();
        }
    }
}
