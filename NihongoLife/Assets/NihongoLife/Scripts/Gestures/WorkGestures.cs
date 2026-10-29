using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Gestures
{
    /// <summary>
    /// Ties the game's work poses (the Animator states "Work_Water", "Work_Hoe" ...) and job actions to the gesture library: the
    /// clip still moves the body, the gesture layer places the hands (watering can tipped over the plot, hoe swung, box carried).
    /// </summary>
    public static class WorkGestures
    {
        /// <summary>Gesture of a work pose (null when the pose has none).</summary>
        public static string ForPose(string pose) => pose switch
        {
            "Work_Hoe" => "farm_hoe",
            "Work_Plant" => "farm_plant",
            "Work_Water" => "water_plants",
            "Work_Harvest" => "farm_harvest",
            "Work_Clear" => "farm_harvest",
            "Work_Feed" => "pickup",
            "Work_Shelf" => "stock_shelf",
            "Work_Carry" => "carry_box",
            _ => null,
        };

        /// <summary>Gesture of a shift action of a job station (carry, stock, scan, serve ...).</summary>
        public static string ForJob(string action) => action switch
        {
            "carry" => "carry_box",
            "stock" => "stock_shelf",
            "scan" => "cashier_scan",
            "pay" => "cashier_pay",
            "serve" => "serve_tray",
            "pickup" => "pickup",
            "welcome" => "welcome_bow",
            "check" => "doctor_check",
            "sweep" => "sweep",
            "mop" => "mop_floor",
            "wipe" => "wipe_table",
            "chop" => "cook_chop",
            "stir" => "cook_stir",
            "flip" => "cook_flip",
            "plate" => "cook_plate",
            "sushi" => "sushi_press",
            "roll" => "sushi_roll",
            "paint" => "paint_canvas",
            _ => null,
        };

        public static bool Start(GameObject who, string gestureId)
        {
            if (who == null || string.IsNullOrEmpty(gestureId)) return false;
            return GestureLayer.On(who).Play(gestureId);
        }

        public static void Stop(GameObject who, float fade = -1f)
        {
            if (who == null) return;
            var layer = who.GetComponent<GestureLayer>();
            if (layer != null) { layer.HideProps(false); layer.Stop(fade); }
        }
    }
}