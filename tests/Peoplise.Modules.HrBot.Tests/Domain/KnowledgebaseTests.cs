using FluentAssertions;
using Peoplise.Modules.HrBot.Domain.Entities;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Domain;

public class KnowledgebaseTests
{
    private static Knowledgebase CreateKnowledgebaseWithOneEntry()
    {
        var kb = new Knowledgebase(Guid.NewGuid());
        var answer = new KnowledgebaseAnswer(Guid.NewGuid(), "We offer 21 days of paid annual leave.");
        var question = new KnowledgebaseQuestion(Guid.NewGuid(), "How much annual leave do you offer?", ["leave", "vacation", "izin"], answer);
        kb.AddQuestion(question);
        return kb;
    }

    [Fact]
    public void FindAnswer_returns_the_answer_when_a_keyword_matches()
    {
        var kb = CreateKnowledgebaseWithOneEntry();

        var answer = kb.FindAnswer("What's your vacation policy?");

        answer.Should().NotBeNull();
        answer!.Text.Should().Contain("21 days");
    }

    [Fact]
    public void FindAnswer_returns_null_when_nothing_matches()
    {
        var kb = CreateKnowledgebaseWithOneEntry();

        var answer = kb.FindAnswer("Do you offer relocation support?");

        answer.Should().BeNull();
    }

    [Fact]
    public void A_question_must_have_at_least_one_keyword()
    {
        var answer = new KnowledgebaseAnswer(Guid.NewGuid(), "Yes.");

        var act = () => new KnowledgebaseQuestion(Guid.NewGuid(), "Some question?", [], answer);

        act.Should().Throw<ArgumentException>();
    }
}
