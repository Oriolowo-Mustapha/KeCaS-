namespace kecas.application.feedback.ReviewFeedback;

/// <summary>POST /feedback/:id/review (FR-21): staff triage, NEW/REVIEWED → REVIEWED/RESOLVED.</summary>
public sealed record ReviewFeedbackCommand(Guid ActorId, Guid FeedbackId, FeedbackStatus Target) : ICommand;
