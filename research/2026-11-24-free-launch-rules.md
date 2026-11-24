# What a free public release needs: hosting terms and rules (2026-11-24)
**Status:** Legal & Compliance with the Builder, Session 27, for `project/strategy.md` (D-048). Findings and questions for the lawyer, not advice or conclusions on what is lawful. Labels: Verified / Reported / Assumption.

## Summary
- **The Azure credit can't host the public tool.** The subscription is the Visual Studio subscriber offer (`quotaId MSDN_2014-09-01`, spending limit Off: Verified by the Chief of Staff with `az rest`, 2026-11-24). Microsoft: "This benefit is for development and testing only." "Production workloads must be run on regular subscriptions." (Verified twice: the offer page and Microsoft Learn, updated 2026-07-01). The credit also ends if the Visual Studio subscription ends.
- **Free doesn't exempt us from LSSI.** It covers unpaid services that are an economic activity for the provider. Whether a free side project counts is for the lawyer. If it does, art. 10.1 requires the operator's name, address, email and NIF on the site: Sam's own, if he runs it as an individual (Verified, BOE).
- **A DPIA before launch is the safe reading:** a co-buyer matcher seems to meet at least two AEPD criteria (profiling; data revealing financial situation, the private budget) (Verified list; applying it is an Assumption).
- **The DSA applies to a small platform too:** a way to report illegal content (art. 16), reasons given when we restrict someone (art. 17), contact points (arts. 11–12) and terms covering moderation (art. 14). Small firms are exempt from arts. 20–28 and the transparency report (Verified, Spanish text on BOE).
- **Charging later changes our status:** fees tied to sales could bring the anti-money-laundering law (Ley 10/2010 art. 2.1.l) and regional agent registers (Cataluña's AICAT: €1M a year liability insurance for online-only agents) into play. A free introduction service seems to sit outside both (inference for the lawyer).

## Findings
| Topic | Finding | Label | Source |
|---|---|---|---|
| Azure credit use | "This benefit is for development and testing only." "Production workloads must be run on regular subscriptions." Instances running continuously over 120 hours may be suspended | Verified | [Offer MS-AZR-0063P](https://azure.microsoft.com/en-us/pricing/offers/ms-azr-0063p/) |
| Azure credit use | "These credits are intended for development and testing workloads only and aren't designed for production use." Benefits end if the Visual Studio subscription "expires or is removed" | Verified | [Microsoft Learn, updated 2026-07-01](https://learn.microsoft.com/en-us/azure/devtest/offer/quickstart-individual-credit) |
| Credit runs out | With the spending limit on, the subscription is suspended for the rest of the billing period; with it off, pay-as-you-go charges | Verified | Offer page (above). Ours is Off (Verified, `az rest`), so overruns are billed, not suspended |
| LSSI scope | Information society services include those "no remunerados por sus destinatarios" if they are an economic activity for the provider | Verified | [BOE, Ley 34/2002, annex a](https://www.boe.es/buscar/act.php?id=BOE-A-2002-13758) |
| LSSI information | Art. 10.1: name, address, email, NIF and more, easy to find and free | Verified | BOE (above) |
| Cookies | Art. 22.2: consent for non-essential cookies; the AEPD guide requires a reject option (2023 update) | Verified / Reported | BOE; [AEPD press note](https://www.aepd.es/prensa-y-comunicacion/notas-de-prensa/aepd-actualiza-guia-cookies-para-adaptarla-a-nuevas-directrices-cepd) |
| LSSI fines | Serious: €30,001–150,000; minor: up to €30,000 | Verified | BOE, arts. 38–39 |
| DPIA | Needed "en la mayoría de los casos" with two or more criteria: profiling; decisions affecting access to a service; data on "situación financiera o de solvencia" | Verified | [AEPD list (PDF)](https://www.aepd.es/documento/listas-dpia-es-35-4.pdf) |
| LOPDGDD | Consent from age 14 (art. 7); layered privacy notice (art. 11); discrimination and fraud as risk factors (art. 28.2); record of processing (art. 31) | Verified | [BOE, LOPDGDD](https://www.boe.es/buscar/act.php?id=BOE-A-2018-16673) |
| DSA duties | Arts. 11, 12, 14, 16, 17, 18 apply; small firms exempt from art. 15 report and arts. 20–28, except giving user numbers on request (art. 24.3) | Verified | [DSA, Spanish text on BOE](https://www.boe.es/buscar/doc.php?id=DOUE-L-2022-81573) (read through a summarising tool: check word for word before the lawyer brief) |
| Equal treatment | Protected grounds include "situación socioeconómica" and "cualquier otra condición o circunstancia personal o social"; covers housing offered to the public and online services (art. 3.1.k, n); fines up to €500,000 | Verified | [BOE, Ley 15/2022](https://www.boe.es/buscar/act.php?id=BOE-A-2022-11589) |
| Anti-money-laundering | Covers "quienes ejerzan profesionalmente actividades de agencia, comisión o intermediación en la compraventa de bienes inmuebles" (and in lettings above €120,000 a year or €10,000 a month in rent) (art. 2.1.l, version in force from 2026-10-01) | Verified | [BOE, Ley 10/2010](https://www.boe.es/buscar/act.php?id=BOE-A-2010-6737) |
| Agents' register (Cataluña) | Mandatory for those who mediate "de manera habitual y retribuida"; occasional unpaid activity excluded; online-only agents need €150,000 per claim and €1M a year of liability insurance plus a €300,000 guarantee | Verified | [Generalitat, Registre d'Agents Immobiliaris de Catalunya](https://tramits.gencat.cat/es/tramits/tramits-temes/Registre_Agents_Immobiliaris_Catalunya) |
| Individual or company | Unlimited personal liability as an individual; limited in an S.L. | Assumption | Not checked |

## Questions for the lawyer (add to Part A of `research/2026-11-20-lawyer-brief.md`)
1. Is a free, unpaid co-buyer matching tool run by an individual an information society service (LSSI)? If so, is there a way to meet art. 10.1 without publishing a home address, or should Sam form an S.L. first?
2. For a free release: is the short DPIA plus the draft privacy notice enough, or is more needed (DPO, prior consultation)?
3. Our DSA position: are we a hosting service, an online platform, or both, and what is the minimum notice-and-action and statement-of-reasons process for a one-person operator?
4. Does users choosing each other count as "private life" under Ley 15/2022, and is the private budget (homes only, never people: D-044) safe?
5. Which monetisation routes would make us an intermediary (AML, regional registers, credit intermediary under Ley 5/2019): agency lead fees, broker referrals, a paid agreement pack with partner lawyers?

## Open questions
- Microsoft for Startups credits: are they allowed for production, and is Sam eligible? (Not checked; Assumption that they are meant for production.)
- What does a regular pay-as-you-go subscription cost for today's set-up? D-028 estimated about €16 a month before VAT for B1 and SQL Basic (Reported, 2026-11-22); the SQL free offer may also be available there (not checked).
