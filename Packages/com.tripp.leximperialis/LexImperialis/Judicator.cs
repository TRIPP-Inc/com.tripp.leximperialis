using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TRIPP.Editor.AssetManagement
{
    public abstract class Judicator : ScriptableObject
    {
        public abstract Judgment Adjudicate(Object accused);


        /// <summary>woop
        /// The accused will be handed over to the Tech-priests to be mind-wiped, reprogrammed, and cybernetically-enhanced to meet TRIPP's standards.
        /// </summary>
        /// <param name="judgment"></param>
        /// <param name="infraction"></param>
        /// <returns></returns>
        public abstract string ServitudeImperpituis(Judgment judgment, Infraction infraction);

        protected void AddToJudgement(Judgment judgment, Infraction infraction)
        {
            if (judgment == null)
            {
                Debug.LogError("Null judgement node provided when attempting to add infractions.");
                return;
            }

            if (judgment.infractions == null)
            {
                judgment.infractions = new List<Infraction>();
            }

            judgment.infractions.Add(infraction);
        }

        protected void RemoveInfraction(Judgment judgment, Infraction infraction)
        {
            judgment.infractions.Remove(infraction);
        }
    }
}