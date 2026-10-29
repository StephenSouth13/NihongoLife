# -*- coding: utf-8 -*-
# 10 exercises per tense. line = prefix|subject|verb|tail|form   (a=affirmative, n=negative, q=question)
# items 1-5 fill (a), 6 fill (n), 7-9 multiple choice (a), 10 reorder (q)
SPEC = {
"present_simple": """
|I|drink|coffee every morning|a
|he|play|football with his friends on Sundays|a
|they|study|Japanese twice a week|a
|my sister|live|in Da Nang|a
|the bus|stop|in front of the school every morning|a
|we|eat|dinner together every evening|n
|Lan|read|the news on her phone every day|a
|you|go|to work by bike|a
|the market|begin|at six o'clock every day|a
|she|take|the train to school every day|q
""",
"present_continuous": """
|I|write|an email right now|a
|she|make|dinner at the moment|a
|they|play|football in the park now|a
|we|study|English this week|a
Look! |the baby|run|to the door|a
|he|work|at the clinic today|n
|my parents|live|with us this month|a
|you|learn|Japanese at the moment|a
Be quiet! |the teacher|give|a test now|a
|Mai|learn|to swim at the moment|q
""",
"present_perfect": """
|I|write|three emails today|a
|she|see|that film twice|a
|they|live|in Hoi An since 2020|a
|we|eat|at this restaurant three times this month|a
|he|learn|fifty kanji so far|a
|I|meet|her parents yet|n
|Tom|buy|a new phone this week|a
|they|give|three concerts this year|a
|she|begin|her new job already|a
|you|take|the medicine yet|q
""",
"present_perfect_continuous": """
|I|wait|for the bus for twenty minutes|a
|she|study|English since seven o'clock|a
|they|play|football all afternoon|a
|he|work|at the same bank for ten years|a
|we|learn|Japanese since January|a
|I|run|in the park lately|n
|you|read|that book all day|a
Nam is tired because |he|work|since six o'clock|a
|they|drink|tea together for two hours|a
|you|work|here for a long time|q
""",
"past_simple": """
|I|go|to the market yesterday|a
|she|buy|a new dress last week|a
|they|eat|sushi two days ago|a
|we|meet|our teacher in 2019|a
|he|write|a letter to his mother last night|a
|I|see|the doctor yesterday|n
|the film|begin|at eight o'clock last night|a
|Lan|give|me a gift on my birthday|a
|you|come|to the party last Saturday|a
|he|take|the early train this morning|q
""",
"past_continuous": """
|I|read|a book at eight o'clock last night|a
|she|make|lunch when I arrived|a
|they|play|football when it started to rain|a
|we|eat|dinner when the lights went out|a
|he|write|an email at nine o'clock yesterday|a
|I|run|in the park when I saw him|n
While I was studying, |my brother|play|music loudly|a
|the children|learn|to swim all afternoon yesterday|a
|you|drink|coffee when I called|a
|she|work|at the clinic last summer|q
""",
"past_perfect": """
When we arrived, |the film|begin|already|a
Before she moved to Japan, |she|see|snow only once|a
I was tired because |I|work|all day|a
By the time the bus came, |we|eat|our lunch|a
Before he took the exam, |he|learn|all the kanji|a
She said that |she|buy|the tickets yet|n
The room was clean because |the staff|make|the beds|a
When I got to the station, |the train|leave|already|a
By 2019, |they|live|in three different cities|a
|you|begin|your homework before I called|q
""",
"past_perfect_continuous": """
When the bus finally came, |he|wait|for an hour|a
They were tired because |they|play|football all day|a
|I|study|for two hours before I took a break|a
Her eyes were red because |she|read|all night|a
His shirt was wet because |he|run|in the sun|a
She was not tired because |she|work|hard that day|n
By the time I called, |they|travel|for three days|a
She was exhausted because |she|teach|all morning|a
When she came home, |the baby|sleep|for two hours|a
|you|wait|long before the doctor called|q
""",
"future_simple": """
|I|call|you tomorrow|a
|she|finish|the project next week|a
|they|come|to our house in two days|a
|we|meet|at the station at six|a
|he|buy|a new phone soon|a
|I|go|to the party tonight|n
I think |she|learn|Japanese very fast|a
Don't worry. |I|help|you with the bags|a
Perhaps |they|travel|to Da Nang next month|a
|you|take|me to the airport tomorrow|q
""",
"future_continuous": """
This time tomorrow, |I|swim|in the sea|a
At nine tonight, |she|work|at the clinic|a
At noon next Sunday, |they|travel|to Hanoi|a
Don't call at eight. |we|eat|dinner|a
This time next week, |he|sleep|on the plane|a
|I|work|at three o'clock tomorrow|n
Tomorrow morning, |the chef|cook|for fifty guests|a
At this time next month, |you|study|at university|a
At six tomorrow morning, |we|run|along the beach|a
|you|go|to the market tomorrow|q
""",
"future_perfect": """
By next Friday, |I|finish|this report|a
By noon, |they|arrive|in Hanoi|a
By 2030, |she|learn|five languages|a
By the time you get home, |I|cook|dinner|a
By the end of the year, |we|save|enough money|a
|he|write|the letter by tomorrow evening|n
By Sunday, |the guests|leave|the hotel|a
By next June, |you|study|here for a year|a
By 8 pm, |I|read|the whole book|a
|you|eat|by the time I arrive|q
""",
"future_perfect_continuous": """
By June, |I|work|here for ten years|a
By noon, |she|study|for four hours|a
By next year, |they|live|in Phu Quoc for a decade|a
By the time he retires, |he|teach|for forty years|a
By 5 pm, |we|wait|for three hours|a
By the end of this week, |he|work|here for a year|n
By dinner time, |the kids|swim|for two hours|a
By next month, |they|play|in this band for five years|a
By 2030, |she|travel|for twenty years|a
|you|learn|Japanese for a year by then|q
""",
}