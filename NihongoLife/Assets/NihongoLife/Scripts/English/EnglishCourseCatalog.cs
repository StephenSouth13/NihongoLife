using System;
using System.Collections.Generic;
using System.Linq;

namespace NihongoLife.English
{
    public enum EnglishActivityType { SentencePatterns, Grammar, GapFill, Reading, Listening }
    public enum EnglishSkill { Listening, Speaking, Reading, Writing }

    [Serializable]
    public sealed class EnglishQuestion
    {
        public string prompt;
        public string context;
        public string spokenText;
        public string[] choices;
        public int correctIndex;
        public string explanationVi;
    }

    [Serializable]
    public sealed class EnglishLesson
    {
        public string id;
        public string level;
        public EnglishSkill skill;
        public EnglishActivityType type;
        public string titleVi;
        public string titleEn;
        public string descriptionVi;
        public string objective;
        public List<EnglishQuestion> questions = new();
    }

    /// <summary>
    /// Original seed curriculum for the general-English route. It deliberately contains no copied IELTS/book text.
    /// Larger catalogues should move to versioned content packages, while keeping this small offline starter course.
    /// </summary>
    public static class EnglishCourseCatalog
    {
        private static readonly IReadOnlyList<EnglishLesson> Lessons = Build();

        public static IReadOnlyList<EnglishLesson> All => Lessons;
        public static IEnumerable<EnglishLesson> ForLevel(string level) => Lessons.Where(x => x.level == level);
        public static EnglishLesson Find(string id) => Lessons.FirstOrDefault(x => x.id == id);

        public static string TypeNameVi(EnglishActivityType type) => type switch
        {
            EnglishActivityType.SentencePatterns => "Mẫu câu",
            EnglishActivityType.Grammar => "Ngữ pháp",
            EnglishActivityType.GapFill => "Điền từ",
            EnglishActivityType.Reading => "Đọc hiểu",
            EnglishActivityType.Listening => "Nghe",
            _ => "Tiếng Anh"
        };

        public static string SkillNameVi(EnglishSkill skill) => skill switch
        {
            EnglishSkill.Listening => "Nghe",
            EnglishSkill.Speaking => "Nói",
            EnglishSkill.Reading => "Đọc",
            EnglishSkill.Writing => "Viết",
            _ => "English"
        };

        private static EnglishQuestion Q(string prompt, string[] choices, int answer, string explanation, string context = "", string spoken = "") => new()
        {
            prompt = prompt, choices = choices, correctIndex = answer, explanationVi = explanation, context = context, spokenText = spoken
        };

        private static EnglishLesson L(string id, string level, EnglishActivityType type, string vi, string en, string description, string objective, params EnglishQuestion[] questions) => new()
        {
            id = id, level = level, skill = ResolveSkill(id, type), type = type, titleVi = vi, titleEn = en, descriptionVi = description, objective = objective, questions = questions.ToList()
        };

        private static EnglishSkill ResolveSkill(string id, EnglishActivityType type)
        {
            if (type == EnglishActivityType.Listening) return EnglishSkill.Listening;
            if (type == EnglishActivityType.Reading) return EnglishSkill.Reading;
            if (id.Contains("speaking") || type == EnglishActivityType.SentencePatterns) return EnglishSkill.Speaking;
            return EnglishSkill.Writing;
        }

        private static IReadOnlyList<EnglishLesson> Build() => new List<EnglishLesson>
        {
            L("en_a1_patterns", "A1", EnglishActivityType.SentencePatterns, "Giao tiếp hằng ngày", "Everyday English",
                "Chào hỏi, giới thiệu và hỏi thông tin đơn giản.", "Dùng được các mẫu I am…, This is… và Can I…?",
                Q("Choose the best introduction.", new[] { "I am Lan. Nice to meet you.", "I Lan nice meeting.", "Me is Lan.", "Lan am I." }, 0, "I am + tên là mẫu giới thiệu cơ bản; Nice to meet you dùng khi gặp lần đầu."),
                Q("You want some water. What do you say?", new[] { "Can I have some water, please?", "Water give me.", "I can water.", "Have you water me?" }, 0, "Can I have… please? là cách yêu cầu lịch sự."),
                Q("Complete: This ___ my friend, Minh.", new[] { "am", "is", "are", "be" }, 1, "This đi với động từ số ít is.")),

            L("en_a1_grammar", "A1", EnglishActivityType.Grammar, "Nền tảng be và hiện tại đơn", "Be & present simple",
                "Phân biệt am/is/are và động từ hiện tại đơn.", "Tạo câu đúng về bản thân và thói quen.",
                Q("My parents ___ in Da Nang.", new[] { "live", "lives", "living", "is live" }, 0, "Chủ ngữ số nhiều dùng động từ nguyên mẫu ở hiện tại đơn."),
                Q("She ___ English every evening.", new[] { "study", "studies", "studying", "studied" }, 1, "She/he/it thêm -s hoặc -es trong hiện tại đơn."),
                Q("We ___ ready for class.", new[] { "am", "is", "are", "be" }, 2, "We đi với are.")),

            L("en_a1_reading", "A1", EnglishActivityType.Reading, "Đọc biển báo và tin nhắn", "Signs & short messages",
                "Hiểu thông tin rất ngắn trong đời sống.", "Tìm người, nơi chốn, thời gian và yêu cầu trực tiếp.",
                Q("NOTICE: Café closed on Monday. When can you NOT visit?", new[] { "Sunday", "Monday", "Tuesday", "Friday" }, 1, "Closed on Monday nghĩa là quán không mở vào thứ Hai."),
                Q("Text: 'Hi Mai, I'm at the bus stop. See you at 8:15.' Where is the writer?", new[] { "At home", "At school", "At the bus stop", "On the train" }, 2, "Câu I'm at the bus stop cho biết vị trí hiện tại."),
                Q("Sign: 'Please leave your shoes here.' What should you do?", new[] { "Buy shoes", "Remove your shoes", "Clean the floor", "Wait outside" }, 1, "Leave your shoes here là để giày lại tại đây.")),

            L("en_a1_listening", "A1", EnglishActivityType.Listening, "Nghe lời chào và thông tin cá nhân", "Greetings & personal details",
                "Nghe tên, quê quán và số liệu đơn giản.", "Nhận ra từ quen thuộc khi người nói chậm và rõ.",
                Q("What is the speaker's name?", new[] { "Anna", "Emma", "Hannah", "Amelia" }, 1, "Người nói tên Emma.", spoken: "Hello. My name is Emma. It is nice to meet you."),
                Q("Where is Daniel from?", new[] { "Canada", "Australia", "England", "Japan" }, 0, "Daniel nói mình đến từ Canada.", spoken: "I'm Daniel. I'm from Canada, but I live in Tokyo now."),
                Q("What number does the speaker say?", new[] { "13", "30", "33", "43" }, 2, "Người nói đọc số thirty-three.", spoken: "My apartment number is thirty-three.")),

            L("en_a2_gapfill", "A2", EnglishActivityType.GapFill, "Điền từ trong đời sống", "Everyday gap fill",
                "Hoàn thành câu về lịch trình và trải nghiệm quen thuộc.", "Chọn từ phù hợp theo nghĩa và cấu trúc.",
                Q("The train leaves ___ half past seven.", new[] { "in", "on", "at", "by" }, 2, "Dùng at trước một thời điểm cụ thể."),
                Q("I have lived here ___ two years.", new[] { "since", "for", "from", "during" }, 1, "For đi với một khoảng thời gian; since đi với mốc bắt đầu."),
                Q("There isn't ___ milk left.", new[] { "many", "some", "any", "few" }, 2, "Any thường dùng với danh từ không đếm được trong câu phủ định.")),

            L("en_a2_listening", "A2", EnglishActivityType.Listening, "Nghe thông tin thiết yếu", "Essential listening",
                "Nghe thông báo ngắn về giờ và địa điểm.", "Bắt được ý chính và chi tiết số liệu.",
                Q("What time does the library close?", new[] { "5:15", "5:30", "6:00", "6:30" }, 1, "Người nói cho biết thư viện đóng cửa lúc 5:30.", spoken: "The library closes at five thirty today, but the study room stays open until six."),
                Q("Where will the group meet?", new[] { "At the station", "Outside the café", "Inside the museum", "At school" }, 1, "Điểm hẹn là bên ngoài quán cà phê.", spoken: "Let's meet outside the café across from the station at nine o'clock."),
                Q("Which platform should passengers use?", new[] { "Platform 2", "Platform 3", "Platform 4", "Platform 5" }, 2, "Thông báo đổi chuyến tàu sang sân ga số 4.", spoken: "Attention please. The train to Brighton will now leave from platform four, not platform two.")),

            L("en_a2_reading", "A2", EnglishActivityType.Reading, "Đọc email và hướng dẫn", "Emails & instructions",
                "Hiểu email ngắn, lịch hẹn và hướng dẫn từng bước.", "Xác định mục đích và chi tiết cụ thể.",
                Q("Email: 'The meeting has moved from Room 2 to Room 5. The time is unchanged.' What changed?", new[] { "The day", "The time", "The room", "The topic" }, 2, "Email nói địa điểm đổi từ phòng 2 sang phòng 5."),
                Q("Instruction: 'After adding the rice, cover the pan and wait ten minutes.' What happens immediately after adding rice?", new[] { "Serve it", "Cover the pan", "Add water", "Turn off the heat" }, 1, "After adding the rice là tín hiệu cho bước kế tiếp: cover the pan."),
                Q("Message: 'I'll be ten minutes late because the bus is delayed.' Why is the writer late?", new[] { "A late bus", "Bad weather", "A meeting", "A wrong address" }, 0, "Because the bus is delayed nêu lý do trực tiếp.")),

            L("en_a2_speaking", "A2", EnglishActivityType.SentencePatterns, "Nói trong tình huống quen thuộc", "Everyday speaking",
                "Chọn và luyện câu phản hồi tự nhiên khi mua sắm, hỏi đường và hẹn gặp.", "Dùng câu hỏi lịch sự và phản hồi ngắn trọn ý.",
                Q("A shop assistant asks: 'Can I help you?'", new[] { "Yes, I'm looking for a blue jacket.", "I help can blue.", "No jacket is helping.", "Looking you yes." }, 0, "I'm looking for… là mẫu tự nhiên để nói món đồ đang tìm."),
                Q("Ask politely for directions to the station.", new[] { "Station where?", "Tell station.", "Could you tell me how to get to the station?", "You know station." }, 2, "Could you tell me…? là câu hỏi gián tiếp lịch sự."),
                Q("Your friend suggests meeting at six. You agree.", new[] { "Six works for me.", "Six is work me.", "I agreeing six.", "At six maybe no yes." }, 0, "…works for me là cách đồng ý thời gian hẹn tự nhiên.")),

            L("en_b1_reading", "B1", EnglishActivityType.Reading, "Đọc hiểu: Học đều mỗi ngày", "Reading: Small daily habits",
                "Đọc một đoạn ngắn và suy luận ý chính.", "Tìm thông tin, ý chính và nghĩa trong ngữ cảnh.",
                Q("What is the writer's main point?", new[] { "Long sessions are always best.", "Regular short practice can be effective.", "Vocabulary is not important.", "Learners should avoid mistakes." }, 1, "Đoạn văn nhấn mạnh tính đều đặn hơn độ dài của một buổi học.",
                    "Many learners wait for a free weekend before studying. However, a focused fifteen-minute session each day is often more useful than one long session a month. Regular contact helps the brain retrieve words faster and makes progress easier to notice."),
                Q("In the passage, retrieve most nearly means…", new[] { "forget", "translate", "bring back to mind", "write down" }, 2, "Retrieve trong ngữ cảnh là nhớ lại hoặc lấy thông tin từ trí nhớ.",
                    "Many learners wait for a free weekend before studying. However, a focused fifteen-minute session each day is often more useful than one long session a month. Regular contact helps the brain retrieve words faster and makes progress easier to notice."),
                Q("Which habit would the writer probably support?", new[] { "Reviewing ten words every morning", "Studying only before a test", "Waiting until motivation appears", "Avoiding difficult material" }, 0, "Ôn một lượng nhỏ mỗi ngày đúng với lập luận của đoạn.")),

            L("en_b1_grammar", "B1", EnglishActivityType.Grammar, "Kể chuyện và trải nghiệm", "Stories & experiences",
                "Dùng quá khứ đơn, hiện tại hoàn thành và mệnh đề quan hệ.", "Phân biệt sự kiện đã kết thúc với trải nghiệm còn liên quan.",
                Q("I ___ that film three times.", new[] { "saw", "have seen", "am seeing", "see" }, 1, "Hiện tại hoàn thành diễn tả trải nghiệm không nêu thời điểm kết thúc cụ thể."),
                Q("When I arrived, the lesson ___.", new[] { "already started", "has already started", "had already started", "starts" }, 2, "Quá khứ hoàn thành diễn tả hành động xảy ra trước một mốc quá khứ khác."),
                Q("The teacher ___ helped me is from Canada.", new[] { "which", "who", "where", "whose" }, 1, "Who thay cho người và làm chủ ngữ của mệnh đề quan hệ.")),

            L("en_b1_listening", "B1", EnglishActivityType.Listening, "Nghe cuộc gọi và phỏng vấn", "Calls & interviews",
                "Theo dõi ý chính của lời nói chuẩn về công việc và đời sống.", "Nghe lý do, kế hoạch và quan điểm được diễn đạt rõ.",
                Q("Why is the caller changing the appointment?", new[] { "She is ill", "Her train was cancelled", "She forgot the time", "The office is closed" }, 1, "Chuyến tàu bị hủy nên cô ấy không thể tới đúng giờ.", spoken: "I'm calling about my appointment this afternoon. My train has been cancelled, so could we move it to tomorrow morning?"),
                Q("What does the interviewee enjoy most?", new[] { "Working alone", "Solving customer problems", "Writing reports", "Travelling abroad" }, 1, "Người nói nhấn mạnh việc giúp khách hàng giải quyết vấn đề.", spoken: "The best part of my job is helping customers solve problems. Every case is different, so I keep learning."),
                Q("What will the team do next?", new[] { "Cancel the project", "Collect user feedback", "Hire a designer", "Raise the price" }, 1, "Bước tiếp theo là thử mẫu và thu thập phản hồi.", spoken: "We've finished the first prototype. Next week, we'll test it with a small group and collect their feedback.")),

            L("en_b1_speaking", "B1", EnglishActivityType.SentencePatterns, "Nói về kinh nghiệm và ý kiến", "Experiences & opinions",
                "Kể lại trải nghiệm và giải thích quan điểm có lý do.", "Nối ý bằng because, although và for example.",
                Q("Which answer best develops an opinion?", new[] { "I prefer online lessons because I can review recordings after class.", "Online lessons yes.", "I prefer because good.", "Recordings class online prefer." }, 0, "Một ý kiến tốt có lựa chọn rõ và lý do cụ thể."),
                Q("Respond to: 'Tell me about a challenge you overcame.'", new[] { "Challenge is difficult.", "I used to fear presentations, but weekly practice made me more confident.", "I overcome yesterday maybe.", "Presentations are challenge." }, 1, "Câu trả lời có bối cảnh, thay đổi và kết quả."),
                Q("Disagree politely.", new[] { "You're wrong.", "That makes no sense.", "I see your point, but I think cost is also important.", "No, absolutely not you." }, 2, "I see your point, but… công nhận ý đối phương trước khi phản biện.")),

            L("en_b2_gapfill", "B2", EnglishActivityType.GapFill, "Từ nối và kết hợp từ", "Linking & collocations",
                "Hoàn thiện lập luận bằng liên từ và collocation tự nhiên.", "Tạo đoạn văn mạch lạc, chính xác hơn.",
                Q("The course is demanding; ___, it is extremely rewarding.", new[] { "therefore", "however", "because", "unless" }, 1, "However thể hiện sự tương phản giữa demanding và rewarding."),
                Q("The policy may have a significant ___ on attendance.", new[] { "impact", "result", "cause", "reason" }, 0, "Have an impact on là một collocation phổ biến."),
                Q("Students are encouraged to take ___ for their own progress.", new[] { "control", "responsibility", "care", "charge of" }, 1, "Take responsibility for là kết hợp từ đúng.")),

            L("en_b2_listening", "B2", EnglishActivityType.Listening, "Nghe quan điểm và thái độ", "Listening for attitude",
                "Nhận biết quan điểm, sự dè dặt và hàm ý.", "Không chỉ nghe từ khóa mà còn hiểu thái độ người nói.",
                Q("How does the speaker feel about the new schedule?", new[] { "Completely opposed", "Cautiously positive", "Uninterested", "Certain it will fail" }, 1, "Cụm might work và if we review it cho thấy thái độ tích cực nhưng thận trọng.", spoken: "The new schedule might work better than the old one, although I'd like us to review it after the first month."),
                Q("What does the speaker imply?", new[] { "The report is finished.", "The deadline should be ignored.", "More evidence is needed.", "The topic must change." }, 2, "Người nói đánh giá cấu trúc tốt nhưng yêu cầu thêm bằng chứng.", spoken: "Your report is clearly organised, but a few of the claims need stronger evidence before you submit it."),
                Q("Why was the proposal delayed?", new[] { "It was too expensive.", "Two departments requested changes.", "The manager was absent.", "The data was lost." }, 1, "Hai bộ phận muốn sửa phần ngân sách.", spoken: "We had hoped to approve the proposal on Friday. However, two departments asked for revisions to the budget section.")),

            L("en_b2_reading", "B2", EnglishActivityType.Reading, "Đọc bài phân tích và báo cáo", "Analysis & reports",
                "Hiểu lập luận dài hơn, quan hệ nguyên nhân và thái độ tác giả.", "Phân biệt dữ kiện, kết luận và giới hạn của bằng chứng.",
                Q("A report finds correlation but warns that other factors may be involved. What is the safest conclusion?", new[] { "One factor definitely caused the other.", "There is an association, but causation is unproven.", "The data is useless.", "Other factors never matter." }, 1, "Correlation cho thấy liên hệ, không tự chứng minh quan hệ nhân quả."),
                Q("The author describes a solution as 'promising, though not without practical obstacles.' What is the attitude?", new[] { "Unconditionally enthusiastic", "Cautiously supportive", "Completely dismissive", "Indifferent" }, 1, "Promising thể hiện ủng hộ; though… obstacles tạo sự dè dặt."),
                Q("Which detail most strengthens a report's conclusion?", new[] { "A memorable anecdote", "A larger replicated study", "A confident title", "An anonymous comment" }, 1, "Nghiên cứu lặp lại với mẫu lớn cung cấp bằng chứng mạnh hơn.")),

            L("en_b2_speaking", "B2", EnglishActivityType.SentencePatterns, "Thảo luận và thuyết trình", "Discussion & presentation",
                "Phát triển lập luận, xử lý phản biện và làm rõ ý.", "Nói mạch lạc bằng signposting và hedging.",
                Q("Introduce the next point in a presentation.", new[] { "Whatever, next.", "Turning now to our second finding…", "Second thing is come.", "I finished first maybe." }, 1, "Turning now to… là signpost giúp người nghe theo dõi cấu trúc."),
                Q("Qualify a generalisation.", new[] { "This is true for everyone.", "This tends to be true in larger cities, although exceptions exist.", "No exceptions ever.", "All people certainly agree." }, 1, "Tends to và although exceptions exist tạo phát biểu có sắc thái."),
                Q("Ask someone to clarify.", new[] { "What are you talking?", "Could you elaborate on what you mean by sustainable?", "Say again better.", "Your meaning is unclear wrong." }, 1, "Could you elaborate on…? là yêu cầu làm rõ trang trọng và lịch sự.")),

            L("en_c1_reading", "C1", EnglishActivityType.Reading, "Đọc hiểu: Công nghệ và sự chú ý", "Reading: Technology & attention",
                "Phân tích lập luận, sắc thái và giả định của tác giả.", "Đọc phản biện thay vì chỉ tìm từ khóa.",
                Q("Which statement best captures the author's position?", new[] { "Technology inevitably destroys attention.", "All interruptions are equally harmful.", "Design and habits shape technology's effect on attention.", "People should stop using digital tools." }, 2, "Tác giả bác bỏ cách nhìn tất định và nhấn mạnh thiết kế cùng thói quen.",
                    "Digital tools are often blamed for fragmenting attention, yet the relationship is not inevitable. A notification system designed around urgency will interrupt; one designed around deliberate review may protect concentration. Individual habits matter too, but treating distraction solely as a failure of willpower lets poor design escape scrutiny."),
                Q("Why does the author mention willpower?", new[] { "To praise disciplined users", "To show that personal blame can hide design problems", "To argue habits do not matter", "To recommend stricter notifications" }, 1, "Câu cuối cho rằng chỉ đổ lỗi cho ý chí sẽ bỏ qua trách nhiệm thiết kế."),
                Q("The tone is best described as…", new[] { "nostalgic", "analytical and qualified", "angry and absolute", "humorous" }, 1, "Lập luận cân bằng nhiều yếu tố và tránh kết luận tuyệt đối.")),

            L("en_c1_patterns", "C1", EnglishActivityType.SentencePatterns, "Diễn đạt học thuật linh hoạt", "Flexible academic expression",
                "Dùng hedging, inversion và cấu trúc nhấn mạnh.", "Viết lập luận có sắc thái thay vì khẳng định tuyệt đối.",
                Q("Choose the most appropriately cautious claim.", new[] { "This proves online learning is always better.", "The findings may indicate a modest benefit for some learners.", "Everyone clearly prefers online classes.", "There can be no alternative explanation." }, 1, "May indicate và for some learners tạo mức độ dè dặt phù hợp văn phong học thuật."),
                Q("Complete the inversion: Rarely ___ such a rapid change.", new[] { "we have seen", "have we seen", "we saw", "did we seen" }, 1, "Sau trạng từ phủ định đầu câu, trợ động từ đảo lên trước chủ ngữ."),
                Q("Choose the cleft sentence that emphasises practice.", new[] { "Practice improves fluency.", "It is regular practice that improves fluency.", "Regular practice is improving.", "What improves is fluent." }, 1, "It is… that… là cấu trúc chẻ dùng để nhấn mạnh.")),

            L("en_c1_listening", "C1", EnglishActivityType.Listening, "Nghe bài giảng và hàm ý", "Lectures & implication",
                "Theo dõi lập luận phức tạp, sự chuyển hướng và ý ngầm.", "Nhận ra stance, concession và kết luận gián tiếp.",
                Q("What is the lecturer's central qualification?", new[] { "The theory is completely false", "The theory is useful but context-dependent", "Context never matters", "No evidence exists" }, 1, "Người nói công nhận giá trị nhưng giới hạn phạm vi áp dụng.", spoken: "The model remains useful as a starting point. That said, its predictions become less reliable when local institutions differ substantially."),
                Q("What does the manager imply?", new[] { "The launch date is fixed", "The team should reconsider the launch date", "Testing is unnecessary", "The product is cancelled" }, 1, "Cách nói would be optimistic ngụ ý thời hạn hiện tại khó thực tế.", spoken: "Calling Friday a realistic launch date would be optimistic, given that two critical tests are still incomplete."),
                Q("Why does the speaker mention the earlier study?", new[] { "To reject all prior research", "To provide historical context and contrast", "To change the topic", "To avoid presenting evidence" }, 1, "Nghiên cứu cũ tạo nền để đối chiếu với kết quả mới.", spoken: "An earlier study reached the opposite conclusion, but it used a much smaller sample. The new data helps explain why those results differed.")),

            L("en_c1_writing", "C1", EnglishActivityType.Grammar, "Viết lập luận học thuật", "Academic argument",
                "Tổ chức luận điểm, tổng hợp bằng chứng và kiểm soát sắc thái.", "Viết chính xác, cô đọng và có lập trường cân bằng.",
                Q("Choose the strongest thesis statement.", new[] { "Technology is a big topic with many things.", "This essay will talk about technology.", "Although automation can displace some roles, its long-term effect depends largely on retraining and policy design.", "Automation is obviously always good." }, 2, "Thesis tốt nêu lập trường cụ thể, có phạm vi và thể hiện quan hệ giữa các ý."),
                Q("Which sentence synthesises two sources?", new[] { "Source A is interesting. Source B is interesting too.", "While both studies report higher productivity, only Chen links the gain to reduced interruptions.", "I read two sources.", "The sources say things." }, 1, "Synthesis vừa chỉ ra điểm chung vừa nêu khác biệt có ý nghĩa."),
                Q("Choose the most concise revision: 'Due to the fact that the sample was small, the findings were limited.'", new[] { "Owing to the existing fact of the small sample, limitations existed.", "Because the sample was small, the findings were limited.", "The findings, due to the fact, had limitations of smallness.", "A small sample was a fact and findings." }, 1, "Because thay cụm dài due to the fact that mà không mất nghĩa."))
        };
    }
}
