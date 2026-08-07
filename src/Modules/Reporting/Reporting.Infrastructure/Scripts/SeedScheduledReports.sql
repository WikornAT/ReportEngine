-- Seed examples for reporting.scheduled_reports
-- Prerequisite: replace ReportDefinitionId values with valid IDs from reporting.report_definitions.

INSERT INTO reporting.scheduled_reports
(
    "Id",
    "ReportDefinitionId",
    "ScheduleType",
    "ParametersJson",
    "RequestedFormatsCsv",
    "TriggeredBy",
    "IsActive",
    "CreatedAt"
)
VALUES
(
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000001',
    'Daily',
    '{}'::jsonb,
    'Pdf',
    'system',
    true,
    now()
),
(
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000002',
    'Weekly',
    '{}'::jsonb,
    'Pdf',
    'system',
    true,
    now()
),
(
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000003',
    'Monthly',
    '{}'::jsonb,
    'Pdf',
    'system',
    true,
    now()
),
(
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000004',
    'Quarterly',
    '{}'::jsonb,
    'Pdf',
    'system',
    true,
    now()
),
(
    gen_random_uuid(),
    '00000000-0000-0000-0000-000000000005',
    'Yearly',
    '{}'::jsonb,
    'Pdf',
    'system',
    true,
    now()
);

-- Example query to get candidate IDs:
-- select "Id", "Name", "Category" from reporting.report_definitions order by "CreatedAt" desc;
