namespace HraBot.Tests.Ai;

public static class IchraQuestionProvider
{
    public static IEnumerable<EvalQuestion> GetEvalQuestions()
    {
        yield return new(
            1,
            "What does ICHRA stand for?",
            "ICHRA stands for Individual Coverage Health Reimbursement Arrangement. It's also commonly referred to as an ICHRA plan."
        );

        yield return new(
            2,
            "What is an ICHRA?",
            "An ICHRA is a type of account-based healthcare plan that allows employers to reimburse employees for individual health insurance premiums and qualified medical expenses tax-free. "
        );
    }
}
