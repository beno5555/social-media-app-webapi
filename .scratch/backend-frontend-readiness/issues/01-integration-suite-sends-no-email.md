# 01: Integration suite sends no email

**What to build:** A developer can run the full integration suite as often as they like and no email leaves their machine. Email sending gains an on/off setting in the email configuration, on by default. When it is off, flows that send email (deactivation, password reset, the failed-registration warning) behave exactly as when a send succeeds, minus the sending. The integration test host switches it off itself, so a fresh clone is safe without touching any gitignored settings file.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** routine: one config setting and one guard in the email sender, following the existing options pattern, with the whole suite as the check.

- [ ] The email configuration has an on/off setting that defaults to on, so existing deployments keep sending without a config change
- [ ] With the setting off, the email sender opens no SMTP connection and reports success to its caller
- [ ] The integration test host forces the setting off, independent of any settings file
- [ ] The full integration suite passes, and the endpoints that send email still return their normal success responses
- [ ] The new setting is named in the config prerequisites list in the project instructions, without any value
- [ ] No interface or fake email sender is introduced
