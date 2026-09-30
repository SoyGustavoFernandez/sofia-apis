namespace SOFIA.Application.Common.Models;

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);
