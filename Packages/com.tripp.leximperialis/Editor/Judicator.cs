using System.Collections.Generic;
using UnityEngine;

namespace TRIPP.LexImperialis.Editor
{
    public abstract class Judicator : ScriptableObject
    {
        /// <summary>
        /// Adjudicates the specified accused object.
        /// </summary>
        /// <param name="accused">The object being accused.</param>
        /// <returns>A Judgment object representing the result of the adjudication.</returns>
        public abstract Judgment Adjudicate(Object accused);

        /// <summary>
        /// Provides a servitude imperpituis string based on the given judgment and infraction.
        /// </summary>
        /// <param name="judgment">The judgment to base the string on.</param>
        /// <param name="infraction">The infraction to base the string on.</param>
        /// <returns>A string representing the servitude imperpituis.</returns>
        public abstract string ServitudeImperpituis(Judgment judgment, Infraction infraction);

        /// <summary>
        /// Creates a new judgment or adds an infraction to an existing judgment.
        /// </summary>
        /// <param name="infraction">The infraction to add.</param>
        /// <param name="judgment">The existing judgment, or null to create a new one.</param>
        /// <param name="accused">The object being accused.</param>
        /// <returns>A Judgment object with the added infraction.</returns>
        protected Judgment CreateOrAddInfractionsToJudgment(Infraction infraction, Judgment judgment, Object accused)
        {
            return CreateOrAddInfractionsToJudgment(new List<Infraction> { infraction }, judgment, accused);
        }

        /// <summary>
        /// Creates a new judgment or adds infractions to an existing judgment.
        /// </summary>
        /// <param name="infractions">The list of infractions to add.</param>
        /// <param name="judgment">The existing judgment, or null to create a new one.</param>
        /// <param name="accused">The object being accused.</param>
        /// <returns>A Judgment object with the added infractions.</returns>
        protected Judgment CreateOrAddInfractionsToJudgment(List<Infraction> infractions, Judgment judgment, Object accused)
        {
            Judgment result = judgment;
            if (infractions != null)
            {
                if (result == null)
                {
                    if (accused == null)
                    {
                        Debug.LogError($"Judicator : {this}\n" +
                            $"Null accused object provided when attempting to create a new judgment.");
                        return result;
                    }

                    result = new Judgment
                    {
                        accused = accused,
                        infractions = new List<Infraction>()
                    };
                }

                result.infractions ??= new List<Infraction>();
                result.infractions.AddRange(infractions);
            }
            else
            {
                Debug.LogError($"Judicator : {this}\n" +
                    $"Null infractions list provided when attempting to create a new judgment.");
            }

            return result;
        }

        /// <summary>
        /// Removes an infraction from the specified judgment.
        /// </summary>
        /// <param name="judgment">The judgment to remove the infraction from.</param>
        /// <param name="infraction">The infraction to remove.</param>
        protected void RemoveInfraction(Judgment judgment, Infraction infraction)
        {
            judgment.infractions.Remove(infraction);
        }
    }
}