using System.Collections.Generic;
using System.Linq;
using Pirates;

namespace Hydra {

    public class Main : IPirateBot {


        //---------------[ Main variables ]-----------
        public static PirateGame game;
        public static List<int> didTurn = new List<int>();
        public static List<Asteroid> asteroidsPushed = new List<Asteroid>();
        public static List<Pirate> piratesPushed = new List<Pirate>();
        public static List<Pirate> holdersPaired = new List<Pirate>();
        public static Dictionary<Pirate, Capsule> capsulesTargetted = new Dictionary<Pirate, Capsule>();
        public static bool debug = true;
        public static bool importantdebug = true;
        //--------------------------------------------


        //---------------[ Mines ]--------------------
        public static List<Location> mines = new List<Location>();
        public static List<Location> enemyMines = new List<Location>();
        public static int maxMiners = 2;
        public static int maxMolers = 2;
        //--------------------------------------------


        //---------------[ Task mangment ]-------------
        public static Dictionary<int, TaskType> tasks = new Dictionary<int, TaskType>();
        public static List<Pirate> unemployedPirates = new List<Pirate>();
        public readonly List<TaskType> todoTasks = new List<TaskType>(new List<TaskType>
        { TaskType.MINER, TaskType.MOLE, TaskType.BOOSTER});
        public static int alivePirateCount = 0;
        //--------------------------------------------


        public void DoTurn(PirateGame game) {

            Main.game = game;

            if (game.Cols < 6000) {
                todoTasks.Add(TaskType.BERSERKER);
            }
            // Clearing objects
            holdersPaired.Clear();
            capsulesTargetted.Clear();
            asteroidsPushed.Clear();
            tasks.Clear();
            didTurn.Clear();
            piratesPushed.Clear();
            alivePirateCount = game.GetMyLivingPirates().Count();
            unemployedPirates = game.GetMyLivingPirates().ToList();

            // Gettings the mines
            if (game.GetMyCapsules().Count() > 0) {
                game.GetMyCapsules().Where(cap => cap.Holder == null && !mines.Contains(cap.Location)).ToList().ForEach(cap => mines.Add(cap.Location));
            }

            if (game.GetEnemyCapsules().Count() > 0) {
                game.GetEnemyCapsules().Where(cap => cap.Holder == null && !enemyMines.Contains(cap.Location)).ToList().ForEach(cap => enemyMines.Add(cap.Location));
            }

            GiveTasks();

            foreach (KeyValuePair<int, TaskType> pair in tasks.Where(pair => pair.Value == TaskType.BOOSTER)) {
                if (!Main.debug) {
                    taskTypeToTask(game.GetMyPirateById(pair.Key), pair.Value).Preform();
                } else {
                    game.Debug(taskTypeToTask(game.GetMyPirateById(pair.Key), pair.Value).Preform());
                }
            }

            foreach (KeyValuePair<int, TaskType> pair in tasks.Where(pair => pair.Value != TaskType.BOOSTER)) {
                if (!Main.debug) {
                    taskTypeToTask(game.GetMyPirateById(pair.Key), pair.Value).Preform();
                } else {
                    game.Debug(taskTypeToTask(game.GetMyPirateById(pair.Key), pair.Value).Preform());
                }
            }

            Predict.Log();
        }


        public Dictionary<Tuple<Pirate, TaskType>, double> GetCurrentCosts() {

            var scores = new Dictionary<Tuple<Pirate, TaskType>, double>();

            foreach (Pirate pirate in unemployedPirates) {
                foreach (TaskType taskType in todoTasks) {

                    Task task = taskTypeToTask(pirate, taskType);
                    double score = task.Bias() + task.GetWeight();

                    scores[new Tuple<Pirate, TaskType>(pirate, taskType)] = score;
                }
            }

            foreach (Pirate pirate in unemployedPirates) {
                var ptasks = from tup in scores.Keys.ToList().Where(tup => tup.Item1.Id == pirate.Id) select tup.Item2.ToString() + " > " + scores[tup] + "  ||  ";
                string s = "";
                ptasks.ToList().ForEach(str => s += str);

                if (Main.importantdebug) {
                    game.Debug(pirate.Id + " | " + s);
                }
            }

            return scores;
        }


        public void GiveTasks() {

            for (int i = 0; i < alivePirateCount; i++) {

                var scores = GetCurrentCosts();
                var sorted = scores.Keys.OrderByDescending(key => scores[key]);

                Pirate pirate = sorted.First().Item1;
                TaskType taskType = sorted.First().Item2;

                tasks[pirate.Id] = taskType;
                unemployedPirates.Remove(pirate);

                if (taskType == TaskType.MINER && Utils.FreeCapsulesByDistance(pirate.Location).Count > 0) {
                    var cloestCapsule = Utils.FreeCapsulesByDistance(pirate.Location).First();
                    capsulesTargetted.Add(pirate, cloestCapsule);
                }

                if (Main.importantdebug) {
                    game.Debug("Gave: " + pirate.Id + " | " + taskType + " at cost: " + scores[sorted.First()]);
                }
            }
        }


        public Task taskTypeToTask(Pirate pirate, TaskType task) {

            switch (task) {
                case TaskType.BERSERKER:
                    return new TaskBerserker(pirate);

                case TaskType.ESCORT:
                    return new TaskEscort(pirate);

                case TaskType.BOOSTER:
                    return new TaskBooster(pirate);

                case TaskType.MOLE:
                    return new TaskMole(pirate);

                default:
                    return new TaskMiner(pirate);
            }
        }

    }

}
