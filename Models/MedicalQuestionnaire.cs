namespace BloodBank.Models
{
    public class MedicalQuestionnaire
    {
        public int Id { get; set; }

        // Relationship
        public int            DonorId { get; set; }
        public virtual Donor  Donor   { get; set; } = null!;

        // Select fields
        public string AgeRange    { get; set; } = string.Empty;
        public string WeightRange { get; set; } = string.Empty;

        // Yes / No Questions
        public bool HadFullMealInLast4Hours             { get; set; }
        public bool HasUncontrolledBloodPressure        { get; set; }
        public bool TakesBloodThinners                  { get; set; }
        public bool HasActiveInfectiousDisease          { get; set; }
        public bool HasChronicHeartOrLungCondition      { get; set; }
        public bool HadSurgeryOrMajorIllnessLast6Months { get; set; }
        public bool IsPregnantOrRecentlyPregnant        { get; set; }
        public bool DonatedWholeBloodLast90Days         { get; set; }
        public bool DonatedPlateletsLast14Days          { get; set; }
        public bool HadBloodTransfusionLast12Months     { get; set; }
        public bool TraveledToMalariaRiskAreaLast12Months { get; set; }
        public bool HadTattooOrPiercingLast6Months      { get; set; }
        public bool HadCovidOrVaccineLast28Days         { get; set; }
    }
}
