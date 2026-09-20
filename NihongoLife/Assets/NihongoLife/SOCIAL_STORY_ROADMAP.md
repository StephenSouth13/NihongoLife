# NihongoLife - Social story and career roadmap

> **Canon cốt truyện**: nội dung và nhân vật lấy từ `Docs/STORY_BIBLE.md` (nhân vật chính là du học sinh Việt tại ひばり日本語学院; các chương 0-4: mở màn, chào hỏi, cửa hàng, xóm giềng và nhà hàng, ga và lễ hội). Tài liệu này mô tả **cách mở khóa và phát triển nhánh** (nghề, quản lý, dạy học), không thay thế bible.
>
> | Act ở đây | Tương ứng trong bible |
> |---|---|
> | Act 1 - New resident | Chương 0-3 (mở màn, trường học, konbini, xóm giềng, nhà hàng) |
> | Act 2 - Working life | Nhánh phụ mở bằng kiến thức: `career.retail` (konbini), `career.station` (ga), `career.education` (trường), `community` (xóm giềng, lễ hội) |
> | Act 3-4 - Responsibility, Teacher | Sau lễ hội (bible mục 4.6: mở rộng mùa thu) |
>
> Kiểm tra hiện trạng và việc còn thiếu: `Docs/PROJECT_AUDIT.md`.

## Product direction

NihongoLife is a Japanese-learning social simulation. Knowledge is the main progression gate. Money, jobs, relationships and exploration create reasons to use Japanese; they must not replace learning.

The same gameplay world supports solo guests, signed-in cloud saves and online social play. Story progress is personal. Shared jobs and player-managed businesses are authoritative online activities.

## Story structure

### Act 1 - New resident

- Learn movement, shopping, hunger, thirst and town etiquette.
- Meet the store clerk, neighbor and station attendant.
- Choose a first part-time route after earning 20 knowledge.
- Choices teach greetings, quantities, directions and polite requests.

### Act 2 - Working life

- Retail route: stock shelves, serve customers, cashier language and complaint handling.
- Station route: ticket help, directions, lost property and platform announcements.
- Education route: assist a class, correct beginner exercises and prepare lesson materials.
- Community route: recycling, neighborhood requests, festivals and helping visitors.
- The player may develop every route; choosing one does not permanently lock the others.

### Act 3 - Responsibility

- Employee: perform guided tasks and learn standard phrases.
- Shift leader: assign NPC tasks, resolve mistakes and train a new employee.
- Manager: set schedules, order stock, review service quality and run learning events.
- Poor decisions reduce reputation; language mistakes trigger correction gameplay, not irreversible failure.

### Act 4 - Teacher and community leader

- Teaching assistant unlocks at 80 knowledge.
- Japanese teacher unlocks at 220 knowledge plus teaching-route requirements.
- Build lessons from mastered vocabulary and grammar.
- Host town workshops and cooperative speaking activities.
- Management and teaching paths converge in a town festival finale with multiple outcomes.

## Branch rules

Every scenario declares `branchId`, `requiredKnowledge`, prerequisite scenarios and unlocked scenarios. `ScenarioCampaignManager.GetAvailableBranches()` returns currently playable branches instead of forcing one linear next mission.

Recommended branch IDs:

- `main`: residence and town story.
- `career.retail`: store employee to manager.
- `career.station`: station assistant to manager.
- `career.education`: teaching assistant to teacher.
- `community`: neighbors, recycling and festivals.
- `online.coop`: optional role-play tasks for signed-in players.

## Career progression implemented

- Separate career record for every job.
- Ranks: Trainee, Employee, Shift Leader and Manager.
- Tracks completed shifts and reputation.
- Shift Leader: 5 shifts, 50 reputation and 80 knowledge.
- Manager: 15 shifts, 160 reputation and 220 knowledge.
- Signed-in progress is saved through the existing Supabase progress repository.
- Guest progress remains session-only.

## Station journey implemented

- Buy a ticket, pass the gate, board, start the ride and leave after arrival.
- Dedicated route/status UI for Sakura to Midori.
- Playable carriage in the existing station scene; no extra scene was created.
- Reused moving scenery is visible through the carriage windows without moving the world.
- Passenger interaction provides a bilingual travel exchange and knowledge reward.
- Final carriage art remains listed in `MISSING_ASSETS.md` because the Train Pack has no interior.

## Business progression implemented

- Wallet, career and business data are part of local/cloud progress.
- Company opening requires 300 knowledge and Y50,000 capital.
- Hiring validates a living-wage floor; underpaying reduces fairness and reputation.
- Partnerships require reputation and educational contribution, not money alone.
- Company, career rank and completed shifts are visible in the character profile.

## Online architecture

Client-authoritative rewards are acceptable only for offline development. Production online jobs require a server-side transaction/RPC that validates:

- authenticated user and active job;
- shift start/end time and cooldown;
- required scenario state;
- submitted learning answers and score;
- salary, reputation and promotion limits;
- idempotency key so one shift cannot be claimed twice.

Supabase tables/RPC planned:

- `player_careers`: user, role, rank, shifts and reputation.
- `organizations`: store/school owner and configuration.
- `organization_members`: manager, employee and teacher permissions.
- `work_shifts`: assigned role, start/end, result and reward status.
- `story_decisions`: personal branching choices and version.
- `complete_work_shift(...)`: security-definer RPC with validation and atomic reward.

Realtime should broadcast presence, role-play actions and organization updates. It must not decide money, knowledge, rank or punishments.

## Management gameplay

Managers can schedule NPC/player shifts, choose stock priorities, assign training and review service scores. They cannot directly edit salaries, knowledge or another player's inventory. Online employee applications require manager approval or an automatic rule configured by the organization owner.

## Current limitations

- Career progress and branching data structures are implemented.
- Store and station job interaction points exist in their scenes.
- Production server RPC, organization UI and manager scheduling UI are not implemented yet.
- Existing story scenarios need metadata and additional nodes for every route.
- Character work animations and final voice files are still asset-dependent.
