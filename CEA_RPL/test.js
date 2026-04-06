
    let currentStep = 1;
    let maxReachedStep = 1; 
    const totalSteps = 15;

    // Helper to set field value safely (used by Resume logic)
    const setVal = (name, val) => {
        if (val === null || val === undefined) return;
        const form = document.getElementById('applicationForm');
        if (!form) return;
        const input = form.querySelector(`[name="${name}"]`);
        if (input) {
            if (input.type === 'checkbox') input.checked = !!val;
            else if (input.type === 'radio') {
                const radio = form.querySelector(`[name="${name}"][value="${val}"]`);
                if (radio) radio.checked = true;
            } else input.value = val;
            
            // Trigger change for fields with dynamic logic (like Gov ID)
            input.dispatchEvent(new Event('change'));
        }
    };
    const stepNames = [
        "Applicant Details",
        "Category of Certification Applied For",
        "Educational Qualifications",
        "Professional Experience",
        "Project Experience",
        "Upload Reports",
        "Details of Certification / Training",
        "Membership in Professional Bodies",
        "Paper Published / Presented",
        "Awards / Recognition",
        "Software / Tools / Application Skills",
        "Other Relevant Enclosures",
        "Payment Details",
        "Declaration by Applicant",
        "Review & Submit"
    ];

    function updateUI() {
        // Force sequential redirect if user tries to bypass
        if (currentStep > maxReachedStep + 1) currentStep = maxReachedStep + 1;

        // Toggle Sections
        document.querySelectorAll('.form-section').forEach((sec, idx) => {
            const stepNum = idx + 1;
            if (stepNum === currentStep) {
                sec.classList.remove('hide');
                sec.classList.add('active');
                sec.querySelector('.section-status').textContent = 'Active';
            } else {
                sec.classList.add('hide');
                sec.classList.remove('active');
                sec.querySelector('.section-status').textContent = (stepNum <= maxReachedStep) ? 'Completed' : 'Pending';
            }
        });

        // Update Sidebar items
        document.querySelectorAll('.nav-item').forEach((item, idx) => {
            const stepNum = idx + 1;
            item.classList.toggle('active', stepNum === currentStep);
            
            if (stepNum <= maxReachedStep) {
                item.classList.add('completed');
                item.classList.remove('locked');
            } else if (stepNum === maxReachedStep + 1) {
                item.classList.remove('locked', 'completed');
            } else {
                item.classList.add('locked');
                item.classList.remove('completed');
            }
        });

        // Progress
        const progress = (currentStep / totalSteps) * 100;
        const fill = document.getElementById('progressBarFill');
        if (fill) fill.style.width = progress + '%';
        
        document.getElementById('currentStepDisplay').textContent = currentStep;
        document.getElementById('currentStepName').textContent = stepNames[currentStep - 1];

        // Buttons
        document.getElementById('btnPrev').style.visibility = (currentStep === 1) ? 'hidden' : 'visible';
        
        // Final Submit Logic
        const nextBtn = document.getElementById('btnNext');
        if (currentStep === totalSteps) {
            nextBtn.style.display = 'none';
            populateReviewSummary(); // Refresh review data when on last step
        } else {
            nextBtn.style.display = 'inline-block';
            nextBtn.textContent = (currentStep === totalSteps - 1) ? 'Go to Review' : 'Next Section';
        }

        // Scroll
        const activeSec = document.getElementById('step-' + currentStep);
        if (activeSec) {
            window.scrollTo({ top: activeSec.offsetTop - 120, behavior: 'smooth' });
        }
    }

    function validateSection(step) {
        const section = document.getElementById('step-' + step);
        if (!section) return true;

        const inputs = section.querySelectorAll('[required]');
        let firstInvalid = null;
        let valid = true;
        
        section.querySelectorAll('.field-error-pulse').forEach(el => el.classList.remove('field-error-pulse'));

        inputs.forEach(input => {
            let isInvalid = false;
            if (input.type === 'checkbox' || input.type === 'radio') {
                if (!input.checked) isInvalid = true;
            } else {
                if (!input.value || input.value.trim() === '') {
                    isInvalid = true;
                } else if (input.type === 'text' || input.tagName === 'TEXTAREA') {
                    // Check meaningful
                    const isDesc = input.tagName === 'TEXTAREA' || input.name === 'enclosure_desc';
                    const skipNames = ['gov_id_number', 'payment_utr', 'mobile', 'alt_mobile', 'email', 'total_experience', 'payment_amount', 'address_perm', 'address_corr'];
                    
                    if (input.name === 'address_perm' || input.name === 'address_corr') {
                        if (input.value.trim() !== '' && !/^[a-zA-Z0-9\s,.\-/#]{3,}$/.test(input.value.trim())) {
                            isInvalid = true;
                            if (!input.parentNode.querySelector('.address-error')) {
                                const err = document.createElement('div');
                                err.className = 'address-error file-validation-hint';
                                err.style.color = '#c53030';
                                err.style.fontSize = '0.75rem';
                                err.style.marginTop = '4px';
                                err.style.fontWeight = '600';
                                err.innerText = "âš ï¸ Address must be at least 3 characters.";
                                input.parentNode.appendChild(err);
                            }
                        } else {
                            if (input.parentNode.querySelector('.address-error')) {
                                input.parentNode.querySelector('.address-error').remove();
                            }
                        }
                    } else if (!skipNames.includes(input.name) && typeof isMeaningfulText === 'function' && !isMeaningfulText(input.value, isDesc)) {
                        isInvalid = true;
                        input.dataset.gibberishError = "true";
                        
                        // Prevent duplicate error additions natively
                        if (!input.parentNode.querySelector('.meaningful-error')) {
                            const err = document.createElement('div');
                            err.className = 'meaningful-error file-validation-hint';
                            err.style.color = '#c53030';
                            err.style.fontSize = '0.75rem';
                            err.style.marginTop = '4px';
                            err.style.fontWeight = '600';
                            err.innerText = "âš ï¸ " + (isDesc ? "Please enter a meaningful description." : "Please enter meaningful information.");
                            // Insert directly after input structurally
                            input.parentNode.appendChild(err);
                        }
                    } else {
                        input.dataset.gibberishError = "";
                        // Remove error dynamically cleanly
                        if (input.parentNode.querySelector('.meaningful-error')) {
                            input.parentNode.querySelector('.meaningful-error').remove();
                        }
                    }
                }
            }

            if (isInvalid) {
                valid = false;
                input.style.borderColor = 'var(--warn)';
                input.classList.add('field-error-pulse');
                if (!firstInvalid) firstInvalid = input;
            } else {
                input.style.borderColor = '';
            }
        });

        // Step 2 specific check
        if (step === 2) {
            const categories = section.querySelectorAll('input[name="cert_category[]"]:checked');
            if (categories.length === 0) {
                valid = false;
                showToast("Please select at least one certification category.", "error");
                const gridElem = section.querySelector('.alert').nextElementSibling;
                if (!firstInvalid) firstInvalid = gridElem;
                gridElem.classList.add('field-error-pulse');
            }
        }

        if (!valid) {
            showToast("Required fields are missing. Please complete them to proceed.", "error");
            if (firstInvalid) {
                const rect = firstInvalid.getBoundingClientRect();
                const absoluteTop = rect.top + window.pageYOffset;
                window.scrollTo({ top: absoluteTop - 180, behavior: 'smooth' });
                try { firstInvalid.focus({ preventScroll: true }); } catch(e) {}
            }
        }
        return valid;
    }

    function nextStep() {
        if (!validateSection(currentStep)) return;
        if (currentStep < totalSteps) {
            maxReachedStep = Math.max(maxReachedStep, currentStep);
            currentStep++;
            updateUI();
        }
    }

    function prevStep() {
        if (currentStep > 1) {
            currentStep--;
            updateUI();
        }
    }

    function showToast(msg, type = 'info') {
        const container = document.getElementById('toast-container');
        if (!container) return;
        const toast = document.createElement('div');
        toast.className = 'toast ' + type;
        toast.textContent = msg;
        container.appendChild(toast);
        setTimeout(() => toast.remove(), 4000);
    }


    function addGenericRow(tableId) {
        const tbody = document.querySelector(`#${tableId} tbody`);
        if (!tbody) return;
        const row = tbody.querySelector('tr').cloneNode(true);
        row.querySelectorAll('input, select').forEach(el => el.value = '');
        tbody.appendChild(row);
    }

    function addEducationRow() {
        const container = document.getElementById('educationContainer');
        const firstRow = container.querySelector('.education-row');
        const newRow = firstRow.cloneNode(true);
        
        // Reset values
        newRow.querySelectorAll('input, select').forEach(el => {
            if (el.type === 'file') {
                el.value = '';
            } else {
                el.value = '';
            }
        });
        
        container.appendChild(newRow);
    }

    function addExperienceRow() {
        const container = document.getElementById('experienceContainer');
        const firstCard = container.querySelector('.experience-card');
        const newCard = firstCard.cloneNode(true);
        
        // Reset values
        newCard.querySelectorAll('input, select, textarea').forEach(el => {
            if (el.type === 'file') {
                el.value = '';
            } else {
                el.value = '';
            }
        });
        
        // Reset dynamic Other Proof options
        newCard.querySelectorAll('.other-opt').forEach(opt => opt.value = 'Other (Please specify)');
        newCard.querySelectorAll('.other-proof-container').forEach(c => {
            c.style.display = 'none';
            c.querySelector('.other-proof-input').removeAttribute('required');
        });
        
        container.appendChild(newCard);
    }

    function toggleOtherProof(selectElement) {
        const wrapper = selectElement.parentElement;
        const container = wrapper.querySelector('.other-proof-container');
        const input = container.querySelector('.other-proof-input');
        const otherOpt = selectElement.querySelector('.other-opt');
        
        if (selectElement.options[selectElement.selectedIndex].classList.contains('other-opt')) {
            container.style.display = 'block';
            input.setAttribute('required', 'required');
            input.focus();
        } else {
            container.style.display = 'none';
            input.removeAttribute('required');
            input.value = '';
            otherOpt.value = 'Other (Please specify)';
        }
    }

    function updateOtherProofValue(inputElement) {
        const wrapper = inputElement.parentElement.parentElement;
        const select = wrapper.querySelector('select[name="exp_designation[]"]');
        const otherOpt = select.querySelector('.other-opt');
        if(inputElement.value.trim() !== '') {
            otherOpt.value = inputElement.value.trim();
        } else {
            otherOpt.value = 'Other (Please specify)';
        }
    }

    // Logic to sync combined duration for professional experience
    document.addEventListener('change', function(e) {
        if (e.target.matches('.exp-start-month, .exp-start-year, .exp-end-month, .exp-end-year')) {
            const card = e.target.closest('.experience-card');
            if (card) {
                const sMonth = card.querySelector('.exp-start-month').value;
                const sYear = card.querySelector('.exp-start-year').value;
                const eMonth = card.querySelector('.exp-end-month').value;
                const eYear = card.querySelector('.exp-end-year').value;
                
                let combined = "";
                if (sMonth && sYear) combined = `${sMonth} ${sYear}`;
                if (combined && (eMonth || eYear)) {
                    combined += " - " + (eMonth === "Present" ? "Present" : `${eMonth} ${eYear}`);
                }
                
                card.querySelector('.exp-duration-hidden').value = combined.trim();
            }
        }
    });

    function addProjectRow() {
        const container = document.getElementById('projectContainerCat1');
        const firstCard = container.querySelector('.project-card');
        const newCard = firstCard.cloneNode(true);
        
        // Update serial number
        const currentCount = container.querySelectorAll('.project-card').length + 1;
        newCard.querySelector('.project-badge').textContent = `Project ${currentCount}`;
        
        // Reset values
        newCard.querySelectorAll('input, select').forEach(el => {
            el.value = '';
        });
        
        container.appendChild(newCard);
    }

    function addTrainingRow() {
        const container = document.getElementById('trainingRowsContainer');
        const firstRow = container.querySelector('.step7-grid');
        const newRow = firstRow.cloneNode(true);
        newRow.querySelectorAll('input, select').forEach(el => el.value = '');
        container.appendChild(newRow);
    }

    function addCertificationRow() {
        const container = document.getElementById('certificationRowsContainer');
        const firstRow = container.querySelector('.step7-grid');
        const newRow = firstRow.cloneNode(true);
        newRow.querySelectorAll('input, select').forEach(el => el.value = '');
        container.appendChild(newRow);
    }

    function toggleMembershipYears(select) {
        const row = select.closest('.membership-grid-7');
        const yearCells = row.querySelectorAll('.membership-year-cell');
        const isExpired = select.value === 'Expired';
        
        yearCells.forEach(cell => {
            if (isExpired) cell.classList.add('hidden');
            else cell.classList.remove('hidden');
        });
        
        syncMembershipDuration(select);
    }

    function syncMembershipDuration(el) {
        const row = el.closest('.membership-grid-7');
        const status = row.querySelector('.membership-status-val').value;
        const toYear = row.querySelector('.membership-to-year-val').value;
        const hiddenInput = row.querySelector('.membership-duration-hidden');
        
        if (status === 'Expired') {
            hiddenInput.value = 'Expired';
        } else {
            hiddenInput.value = `Active${toYear ? ' (To: ' + toYear + ')' : ''}`;
        }
    }

    function addMembershipRow() {
        const container = document.getElementById('membershipContainer');
        const firstRow = container.querySelector('.membership-row');
        const newRow = firstRow.cloneNode(true);
        
        // Reset serial
        const count = container.querySelectorAll('.membership-row').length + 1;
        newRow.querySelector('.membership-serial').textContent = count;
        
        // Reset values
        newRow.querySelectorAll('input, select').forEach(el => el.value = '');
        newRow.querySelectorAll('.membership-year-cell').forEach(c => c.classList.remove('hidden'));
        
        container.appendChild(newRow);
    }

    function addPublicationRow() {
        const container = document.getElementById('publicationContainer');
        const firstRow = container.querySelector('.publication-row');
        const newRow = firstRow.cloneNode(true);
        newRow.querySelectorAll('input').forEach(el => el.value = '');
        container.appendChild(newRow);
    }

    function updateWordCount(el) {
        const text = el.value.trim();
        const words = text ? text.split(/\s+/).length : 0;
        const maxWords = 200;
        const label = document.getElementById('wordCountLabel');
        
        if (words > maxWords) {
            // Trim the text to 200 words
            const trimmedText = text.split(/\s+/).slice(0, maxWords).join(' ');
            el.value = trimmedText;
            label.textContent = `${maxWords} / ${maxWords} words`;
            label.className = 'word-counter-label danger';
        } else {
            label.textContent = `${words} / ${maxWords} words`;
            if (words > 180) label.className = 'word-counter-label warning';
            else label.className = 'word-counter-label';
        }
    }

    // --- STEP 13: PAYMENT & FEE LOGIC ---
    const CATEGORY_FEE = 5000;

    function calculateTotalFee() {
        const categories = document.querySelectorAll('input[name="cert_category[]"]:checked');
        const count = categories.length;
        const total = count * CATEGORY_FEE;
        
        const amountInput = document.getElementById('payment_amount');
        const displayTotal = document.getElementById('totalFeeDisplay');
        const breakdown = document.getElementById('feeBreakdownLines');
        
        if (amountInput) amountInput.value = total;
        if (displayTotal) displayTotal.textContent = `â‚¹${total.toLocaleString('en-IN', { minimumFractionDigits: 2 })}`;
        
        if (breakdown) {
            breakdown.innerHTML = '';
            categories.forEach(cat => {
                const line = document.createElement('div');
                line.className = 'fee-item';
                line.innerHTML = `<span>${cat.value}</span><span>â‚¹${CATEGORY_FEE.toLocaleString('en-IN')}</span>`;
                breakdown.appendChild(line);
            });
            if (count === 0) {
                breakdown.innerHTML = '<div class="fee-item"><span>No categories selected</span><span>â‚¹0</span></div>';
            }
        }
        
        validatePaymentStep();
    }

    function validatePaymentStep() {
        if (currentStep === 13) {
            const utr = document.getElementById('payment_utr').value.trim();
            const date = document.getElementById('payment_date').value;
            const receipt = document.getElementById('payment_receipt').files.length > 0;
            const amount = parseFloat(document.getElementById('payment_amount').value) || 0;

            const nextBtn = document.getElementById('btnNext');
            
            // Payment is valid if all fields are filled AND amount > 0
            const isValid = utr !== '' && date !== '' && receipt && amount > 0;
            
            if (nextBtn) {
                nextBtn.disabled = !isValid;
                nextBtn.style.opacity = isValid ? '1' : '0.5';
                nextBtn.style.cursor = isValid ? 'pointer' : 'not-allowed';
            }
        }
    }

    // Attach listeners to category checkboxes in Step 2
    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('input[name="cert_category[]"]').forEach(cb => {
            cb.addEventListener('change', calculateTotalFee);
        });
        
        // Initial calc
        calculateTotalFee();
    });

    // --- DRAFT & PERSISTENCE LOGIC ---
    let autoSaveTimeout;
    let draftData = null;

    async function saveDraft() {
        const statusEl = document.getElementById('saveStatus');
        const saveBtn = document.getElementById('btnSaveDraft');
        const oldText = saveBtn ? saveBtn.textContent : 'Save Draft';
        
        if (statusEl) {
            statusEl.textContent = 'Saving draft...';
            statusEl.style.opacity = '1';
        }

        if (saveBtn) {
            saveBtn.disabled = true;
            saveBtn.textContent = 'Saving...';
        }

        const formData = new FormData(document.getElementById('applicationForm'));
        formData.append('current_step', currentStep);

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        try {
            const response = await fetch('/Application/SaveDraft', {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': token },
                body: formData
            });
            const textRaw = await response.text();
            let result;
            try {
                result = JSON.parse(textRaw);
            } catch(e) {
                console.error("Non-JSON Server Error Output:", textRaw);
                throw new Error("Invalid format returned by server.");
            }

            if (response.ok) {
                if (statusEl) {
                    statusEl.textContent = `Draft saved at ${result.savedAt || new Date().toLocaleTimeString()}`;
                    setTimeout(() => { statusEl.style.opacity = '0.7'; }, 4000);
                }
            } else {
                console.error('Save Draft Server Rejection:', result.message);
                if (statusEl) statusEl.textContent = 'Save failed (Invalid Data)';
            }
        } catch (error) {
            console.error('Save draft failed:', error);
            if (statusEl) statusEl.textContent = 'Save failed (Check Connection)';
        } finally {
            if (saveBtn) {
                saveBtn.disabled = false;
                saveBtn.textContent = oldText;
            }
        }
    }

    function autoSave() {
        clearTimeout(autoSaveTimeout);
        autoSaveTimeout = setTimeout(saveDraft, 2000); // Save 2s after typing stops
    }

    async function checkDraftOnLoad() {
        try {
            const response = await fetch('/get-draft');
            if (response.ok) {
                draftData = await response.json();
                const prompt = document.getElementById('resumePromptText');
                if (prompt && draftData.last_saved_at) {
                    prompt.innerHTML = `We found an incomplete application saved on <strong>${draftData.last_saved_at}</strong>. Would you like to continue from <strong>Step ${draftData.current_step}</strong> or start fresh?`;
                }
                document.getElementById('resumeOverlay').classList.remove('hide');
            }
        } catch (e) { console.log('No draft found'); }
    }

    async function confirmResume() {
        if (!draftData) return;
        
        const btn = document.getElementById('btnResume');
        const oldText = btn.textContent;
        btn.disabled = true;
        btn.textContent = 'Resuming...';

        try {
            // 1. Restore current step
            currentStep = draftData.current_step || 1;
            maxReachedStep = Math.max(maxReachedStep, currentStep);
            
            // 2. Populate basic fields
            const form = document.getElementById('applicationForm');
        
        // Segregate restorations into try-catch blocks so one failure doesn't break everything
        const safeRestore = (name, fn) => { try { fn(); } catch(e) { console.warn(`Restore failed for ${name}:`, e); } };

        safeRestore('Basic Fields', () => {
            setVal('title', draftData.title);
            setVal('full_name', draftData.full_name);
            setVal('email', draftData.email);
            setVal('mobile', draftData.mobile);
            setVal('alt_mobile', draftData.alt_mobile);
            setVal('gender', draftData.gender);
            setVal('dob', draftData.dob);
            setVal('parent_relation', draftData.parent_relation);
            setVal('parent_name', draftData.parent_name);
            setVal('citizenship', draftData.citizenship);
            setVal('address_perm', draftData.address_perm);
            setVal('address_corr', draftData.address_corr);
            setVal('gov_id_type', draftData.gov_id_type);
            setVal('other_gov_id_type', draftData.other_gov_id_type);
            setVal('gov_id_number', draftData.gov_id_number);
            setVal('enclosure_desc', draftData.enclosure_desc);

            // Trigger UI logic
            if (typeof toggleGovIdUpload === 'function') toggleGovIdUpload();
        });

        safeRestore('Categories', () => {
            if (draftData.categories) {
                document.querySelectorAll('input[name="cert_category[]"]').forEach(cb => {
                    cb.checked = draftData.categories.includes(cb.value);
                });
            }
        });

        safeRestore('Education', () => {
            if (draftData.educations && draftData.educations.length > 0) {
                const eduContainer = document.getElementById('educationContainer');
                if (eduContainer) {
                    const existingRows = eduContainer.querySelectorAll('.education-row');
                    for (let i = existingRows.length; i < draftData.educations.length; i++) addEducationRow();
                    const rows = eduContainer.querySelectorAll('.education-row');
                    draftData.educations.forEach((edu, i) => {
                        const row = rows[i];
                        if (row) {
                            row.querySelector('[name="edu_degree[]"]').value = edu.degree || "";
                            row.querySelector('[name="edu_discipline[]"]').value = edu.discipline || "";
                            row.querySelector('[name="edu_institution[]"]').value = edu.institution || "";
                            row.querySelector('[name="edu_year[]"]').value = edu.year || "";
                        }
                    });
                }
            }
        });

        safeRestore('Experience', () => {
            if (draftData.experiences && draftData.experiences.length > 0) {
                const expContainer = document.getElementById('experienceContainer');
                if (expContainer) {
                    const existingCards = expContainer.querySelectorAll('.experience-card');
                    for (let i = existingCards.length; i < draftData.experiences.length; i++) addExperienceRow();
                    const cards = expContainer.querySelectorAll('.experience-card');
                    draftData.experiences.forEach((exp, i) => {
                        const card = cards[i];
                        if (card) {
                            card.querySelector('[name="exp_org[]"]').value = exp.org || "";
                            card.querySelector('[name="exp_designation[]"]').value = exp.designation || "";
                            card.querySelector('[name="exp_nature[]"]').value = exp.nature || "";
                            const durationHidden = card.querySelector('.exp-duration-hidden');
                            if (durationHidden) durationHidden.value = exp.duration || "";
                        }
                    });
                }
            }
        });

        safeRestore('Projects', () => {
            if (draftData.projects && draftData.projects.length > 0) {
                const projContainer = document.getElementById('projectContainerCat1');
                if (projContainer) {
                    const existing = projContainer.querySelectorAll('.project-card');
                    for (let i = existing.length; i < draftData.projects.length; i++) addProjectRow();
                    const cards = projContainer.querySelectorAll('.project-card');
                    draftData.projects.forEach((proj, i) => {
                        const card = cards[i];
                        if (card) {
                            card.querySelector('[name="project_name_cat1[]"]').value = proj.name || "";
                            card.querySelector('[name="project_client_cat1[]"]').value = proj.client || "";
                            card.querySelector('[name="project_location_cat1[]"]').value = proj.location || "";
                            card.querySelector('[name="project_year_cat1[]"]').value = proj.year || "";
                            card.querySelector('[name="project_role_cat1[]"]').value = proj.role || "";
                        }
                    });
                }
            }
        });

        safeRestore('Trainings', () => {
            if (draftData.trainings && draftData.trainings.length > 0) {
                const tCont = document.getElementById('trainingRowsContainer');
                const cCont = document.getElementById('certificationRowsContainer');
                
                // For simplicity, we fill existing and add to the first container if overflow.
                // But Step 7 uses a shared name for two tables. We'll handle order.
                const topicInputs = document.querySelectorAll('#step-7 [name="training_name[]"]');
                const fromInputs = document.querySelectorAll('#step-7 [name="training_from[]"]');
                const durInputs = document.querySelectorAll('#step-7 [name="training_duration[]"]');
                const yearInputs = document.querySelectorAll('#step-7 [name="training_year[]"]');
                
                draftData.trainings.forEach((t, i) => {
                    if (topicInputs[i]) {
                        topicInputs[i].value = t.name || "";
                        if (fromInputs[i]) fromInputs[i].value = t.from || "";
                        if (durInputs[i]) durInputs[i].value = t.duration || "";
                        if (yearInputs[i]) yearInputs[i].value = t.year || "";
                    }
                });
            }
        });

        safeRestore('Memberships', () => {
            if (draftData.memberships && draftData.memberships.length > 0) {
                const container = document.getElementById('membershipContainer');
                const existing = container.querySelectorAll('.membership-row');
                for (let i = existing.length; i < draftData.memberships.length; i++) addMembershipRow();
                const rows = container.querySelectorAll('.membership-row');
                draftData.memberships.forEach((m, i) => {
                    const row = rows[i];
                    if (row) {
                        row.querySelector('[name="membership_name[]"]').value = m.name || "";
                        row.querySelector('[name="membership_from[]"]').value = m.from || "";
                        row.querySelector('[name="membership_year[]"]').value = m.year || "";
                        const dur = row.querySelector('.membership-duration-hidden');
                        if (dur) dur.value = m.duration || "";
                    }
                });
            }
        });

        safeRestore('Awards', () => {
             if (draftData.awards && draftData.awards.length > 0) {
                const tbody = document.querySelector('#awardTable tbody');
                const existing = tbody.querySelectorAll('tr');
                for (let i = existing.length; i < draftData.awards.length; i++) addGenericRow('awardTable');
                const rows = tbody.querySelectorAll('tr');
                draftData.awards.forEach((a, i) => {
                    const row = rows[i];
                    if (row) {
                        row.querySelector('[name="award_name[]"]').value = a.name || "";
                        row.querySelector('[name="award_from[]"]').value = a.from || "";
                        row.querySelector('[name="award_year[]"]').value = a.year || "";
                    }
                });
            }
        });

        safeRestore('Software Skills', () => {
            if (draftData.software_skills && draftData.software_skills.length > 0) {
                const tbody = document.querySelector('#softwareTable tbody');
                const existingRows = tbody.querySelectorAll('tr');
                for (let i = existingRows.length; i < draftData.software_skills.length; i++) addGenericRow('softwareTable');
                const rows = tbody.querySelectorAll('tr');
                draftData.software_skills.forEach((skill, i) => {
                    const row = rows[i];
                    if (row) {
                        row.querySelector('[name="software_skill[]"]').value = skill.skill || "";
                        row.querySelector('[name="proficiency_level[]"]').value = skill.level || "";
                    }
                });
            }
        });

        safeRestore('Payment', () => {
            if (draftData.payment) {
                setVal('payment_amount', draftData.payment.amount);
                setVal('payment_date', draftData.payment.date);
                setVal('payment_utr', draftData.payment.utr);
                if (typeof calculateTotalFee === 'function') calculateTotalFee();
            }
        });

        } catch(e) {
            console.error('Critical error during resume:', e);
            showToast("Resume encountered errors, but some data may have been restored.", "error");
        } finally {
            // ALWAYS hide the modal first to unblock the UI
            const overlay = document.getElementById('resumeOverlay');
            if (overlay) overlay.classList.add('hide');
            
            // Re-enable button
            btn.disabled = false;
            btn.textContent = oldText;

            // Safe UI updates
            try {
                updateUI();
                showToast("Application restored from draft.", "success");
            } catch(uiError) {
                console.error('UI Update failed after resume:', uiError);
            }
        }
    }

    function startNew() {
        if (confirm("Are you sure you want to start a new application? This will discard your current draft.")) {
            document.getElementById('resumeOverlay').classList.add('hide');
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        const isSubmitted = @(isSubmitted ? "true" : "false");
        
        if (isSubmitted) {
            // Submitted Mode: Fetch data and show read-only summary
            fetch('/get-draft')
                .then(r => r.json())
                .then(data => {
                    restoreDraft(data); // Fill the form (hidden)
                    populateReviewSummary(true); // Populate summary in read-only mode
                    
                    // Final tweak: If on dashboard, show progress as 100%
                    document.getElementById('progressBarFill').style.width = '100%';
                });
        } else {
            // Draft Mode: Normal behavior
            checkDraftOnLoad();
            
            // Attach auto-save to all inputs
            const form = document.getElementById('applicationForm');
            if (form) {
                form.addEventListener('input', autoSave);
                form.addEventListener('change', autoSave);
            }

            // Bind Final Submit
            const finalBtn = document.getElementById('submitFinalApplicationBtn');
            if (finalBtn) finalBtn.onclick = submitFinalApplication;
        }
    });

    async function submitFinalApplication() {
        console.log("[SUBMIT DEBUG] Submit button clicked. Starting validation...");
        // 1. Mandatory Steps Integrity Check (Scans all 14 steps for missing required fields)
        for (let i = 1; i < totalSteps; i++) {
            if (!validateSection(i)) {
                console.warn(`[SUBMIT DEBUG] Validation failed at Step ${i}`);
                currentStep = i;
                updateUI();
                showToast(`Your application is incomplete at Step ${i}. Please complete all mandatory fields.`, "error");
                return;
            }
        }

        console.log("[SUBMIT DEBUG] All steps validated successfully. Checking mandatory confirmations...");
        // 2. Checklist confirmation check
        const mandatoryCbs = document.querySelectorAll('.mandatory-cb');
        if (Array.from(mandatoryCbs).some(cb => !cb.checked)) {
            console.warn("[SUBMIT DEBUG] Validation failed at Mandatory Checklist Checklist");
            showToast("Please confirm all mandatory documents are uploaded in the checklist (Step 15).", "error");
            document.getElementById('checklistError').style.display = 'block';
            return;
        }

        if (!confirm("Are you sure you want to submit your application? No further changes can be made.")) {
             console.log("[SUBMIT DEBUG] User cancelled submit confirmation.");
             return;
        }

        console.log("[SUBMIT DEBUG] Confirmation accepted. Locking UI and packing payload.");
        const btn = document.getElementById('submitFinalApplicationBtn');
        const oldText = btn.textContent;
        btn.disabled = true;
        btn.style.pointerEvents = 'none';
        btn.style.opacity = '0.7';
        btn.textContent = "Submitting...";

        try {
            const form = document.getElementById('applicationForm');
            if (!form) {
                 console.error("[SUBMIT DEBUG] Form 'applicationForm' not found in DOM!");
                 showToast("Internal Error: Form missing.", "error");
                 btn.disabled = false;
                 btn.style.pointerEvents = 'auto';
                 btn.style.opacity = '1';
                 btn.textContent = oldText;
                 return;
            }
            
            const formData = new FormData(form);
            formData.append('isFinalSubmit', 'true');
            formData.set('current_step', '15');
            
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            console.log("[SUBMIT DEBUG] Payload packed successfully. Sending POST to /Application/Submit ...");

            const response = await fetch('/Application/Submit', {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': token },
                body: formData
            });

            console.log(`[SUBMIT DEBUG] Server responded with status: ${response.status}`);
            const textRaw = await response.text();
            let result;
            try {
                result = JSON.parse(textRaw);
            } catch(e) {
                console.error("[SUBMIT DEBUG] Server response crashed / generated HTML instead of JSON:", textRaw);
                showToast("Server rejected submission format. Check console logs.", "error");
                btn.disabled = false;
                btn.style.pointerEvents = 'auto';
                btn.style.opacity = '1';
                btn.textContent = oldText;
                return;
            }
            console.log("[SUBMIT DEBUG] Server Response Data:", result);

            if (response.ok) {
                // Show unified success overlay
                console.log("[SUBMIT DEBUG] Submit successful. Overlay activated.");
                const overlay = document.getElementById('successOverlay');
                overlay.style.display = 'flex';
                overlay.classList.add('active');
                setTimeout(() => {
                     window.location.href = "/Auth/Dashboard"; // Adjust path as needed or fallback to root
                }, 3000);
            } else {
                // Surface specific backend error messages (e.g., OTP not verified)
                console.error("[SUBMIT DEBUG] Submit rejected by server:", result.message);
                showToast(result.message || "Submission failed due to invalid data. Please check all fields.", "error");
                btn.disabled = false;
                btn.style.pointerEvents = 'auto';
                btn.style.opacity = '1';
                btn.textContent = oldText;
            }
        } catch (error) {
            console.error('[SUBMIT DEBUG] Networking/JS error executing submit:', error);
            showToast("Connection failed or internal error occurred. See console logs.", "error");
            btn.disabled = false;
            btn.style.pointerEvents = 'auto';
            btn.style.opacity = '1';
            btn.textContent = oldText;
        }
    }

    function validateChecklistState() {
        const mandatoryCbs = document.querySelectorAll('.mandatory-cb');
        const submitBtn = document.getElementById('submitFinalApplicationBtn');
        const errorMsg = document.getElementById('checklistError');
        
        const allChecked = Array.from(mandatoryCbs).every(cb => cb.checked);
        
        if (submitBtn) {
            submitBtn.disabled = !allChecked;
            submitBtn.style.opacity = allChecked ? '1' : '0.6';
            submitBtn.style.cursor = allChecked ? 'pointer' : 'not-allowed';
        }
        
        if (errorMsg) {
            errorMsg.style.display = allChecked ? 'none' : 'block';
        }
    }

    function populateReviewSummary(isReadOnly = false) {
        const summary = document.getElementById(isReadOnly ? 'readOnlySummary' : 'reviewSummary');
        if (!summary) return;

        const getData = (name) => document.querySelector(`[name="${name}"]`)?.value || 'N/A';
        const getCheckboxes = (name) => {
            const checked = Array.from(document.querySelectorAll(`input[name="${name}"]:checked`)).map(i => i.value);
            return checked.length > 0 ? checked.join(', ') : 'None selected';
        };
        const getFileStatus = (name) => {
            const input = document.querySelector(`input[name="${name}"]`);
            // Check for actual files or if restoring from data (which we don't have file names for yet in JS)
            const isUploaded = (input && input.files && input.files.length > 0);
            return isUploaded ? 
                '<span style="color: var(--success); font-weight: 600;">âœ“ Uploaded</span>' : 
                '<span style="color: var(--warn); font-size: 0.85rem;">Not Provided / Saved</span>';
        };

        const editLink = (step) => isReadOnly ? '' : `<a href="#" onclick="event.preventDefault(); currentStep=${step}; updateUI();" class="edit-link">Edit</a>`;

        let html = `
            <!-- Step 1: Applicant Details -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 1: Applicant Information</h4>
                    ${editLink(1)}
                </div>
                <div class="review-dl">
                    <div><dt>Full Name</dt><dd>${getData('title')} ${getData('full_name')}</dd></div>
                    <div><dt>Parent/Guardian</dt><dd>(${getData('parent_relation')}) ${getData('parent_name')}</dd></div>
                    <div><dt>Date of Birth</dt><dd>${getData('dob')}</dd></div>
                    <div><dt>Gender</dt><dd>${getData('gender')}</dd></div>
                    <div><dt>Citizenship</dt><dd>${getData('citizenship')}</dd></div>
                    <div><dt>Email</dt><dd>${getData('email')}</dd></div>
                    <div><dt>Mobile</dt><dd>${getData('mobile')} ${getData('alt_mobile') !== 'N/A' && getData('alt_mobile') ? '/ '+getData('alt_mobile') : ''}</dd></div>
                    <div><dt>Gov ID Type</dt><dd>${getData('gov_id_type')}</dd></div>
                    <div><dt>ID Number</dt><dd>${getData('gov_id_number')}</dd></div>
                    <div><dt>ID Proof</dt><dd>${getFileStatus('gov_id_upload')}</dd></div>
                    <div><dt>Photograph</dt><dd>${getFileStatus('applicant_photo')}</dd></div>
                    <div style="grid-column: 1 / -1;"><dt>Permanent Address</dt><dd>${getData('address_perm')}</dd></div>
                    <div style="grid-column: 1 / -1;"><dt>Correspondence Address</dt><dd>${getData('address_corr')}</dd></div>
                </div>
            </div>

            <!-- Step 2: Category Selection -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 2: Category Selection</h4>
                    ${editLink(2)}
                </div>
                <div class="review-dl">
                    <div><dt>Certification Categories</dt><dd>${getCheckboxes('cert_category[]')}</dd></div>
                </div>
            </div>

            <!-- Step 3: Education Summary -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 3: Educational Qualifications</h4>
                    ${editLink(3)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Degree</th><th>Discipline</th><th>Institution</th><th>Year</th><th>Doc</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#educationContainer .education-row')).map(row => {
                                const deg = row.querySelector('[name="edu_degree[]"]')?.value;
                                if (!deg) return '';
                                return `<tr>
                                    <td>${deg}</td>
                                    <td>${row.querySelector('[name="edu_discipline[]"]')?.value || '-'}</td>
                                    <td>${row.querySelector('[name="edu_institution[]"]')?.value || '-'}</td>
                                    <td>${row.querySelector('[name="edu_year[]"]')?.value || '-'}</td>
                                    <td>${getFileStatus('edu_cert[]')}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="5" style="text-align:center; padding:12px; color:var(--ink-light);">No education records added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 4: Professional Experience -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 4: Professional Experience</h4>
                    ${editLink(4)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Organization</th><th>Designation</th><th>Duration</th><th>Role</th><th>Doc</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#experienceContainer .experience-card')).map(card => {
                                const org = card.querySelector('[name="exp_org[]"]')?.value;
                                if (!org) return '';
                                return `<tr>
                                    <td>${org}</td>
                                    <td>${card.querySelector('[name="exp_designation[]"]')?.value || '-'}</td>
                                    <td>${card.querySelector('.exp-duration-hidden')?.value || '-'}</td>
                                    <td>${card.querySelector('[name="exp_nature[]"]')?.value || '-'}</td>
                                    <td>${getFileStatus('exp_proof[]')}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="5" style="text-align:center; padding:12px; color:var(--ink-light);">No experience records added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 5: Projects -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 5: Project Experience</h4>
                    ${editLink(5)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Project Name</th><th>Client</th><th>Location</th><th>Year</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#projectContainerCat1 .project-card')).map(card => {
                                const name = card.querySelector('[name="project_name_cat1[]"]')?.value;
                                if (!name) return '';
                                return `<tr>
                                    <td>${name}</td>
                                    <td>${card.querySelector('[name="project_client_cat1[]"]')?.value || '-'}</td>
                                    <td>${card.querySelector('[name="project_location_cat1[]"]')?.value || '-'}</td>
                                    <td>${card.querySelector('[name="project_year_cat1[]"]')?.value || '-'}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="4" style="text-align:center; padding:12px; color:var(--ink-light);">No projects added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 6: Environmental Audit Reports -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 6: Audit Reports</h4>
                    ${editLink(6)}
                </div>
                <div class="review-dl">
                    <div><dt>Report 1</dt><dd>${getFileStatus('audit_report')}</dd></div>
                    <div><dt>Report 2</dt><dd>${getFileStatus('audit_report')}</dd></div>
                </div>
            </div>

            <!-- Step 8: Training & Certification -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 8: Training & Certification</h4>
                    ${editLink(8)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Name</th><th>Institution</th><th>Year</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#step-8 [name="training_name[]"]')).map((inp, i) => {
                                if (!inp.value) return '';
                                return `<tr>
                                    <td>${inp.value}</td>
                                    <td>${document.querySelectorAll('#step-8 [name="training_from[]"]')[i]?.value || '-'}</td>
                                    <td>${document.querySelectorAll('#step-8 [name="training_year[]"]')[i]?.value || '-'}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="3" style="text-align:center; padding:12px; color:var(--ink-light);">No records added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 13: Payment Info -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 13: Payment Details</h4>
                    ${editLink(13)}
                </div>
                <div class="review-dl">
                    <div><dt>Amount Paid</dt><dd>â‚¹ ${getData('payment_amount')}</dd></div>
                    <div><dt>Payment Date</dt><dd>${getData('payment_date')}</dd></div>
                    <div><dt>UTR / Ref Number</dt><dd>${getData('payment_utr')}</dd></div>
                    <div><dt>Receipt Proof</dt><dd>${getFileStatus('payment_receipt')}</dd></div>
                </div>
            </div>

            <!-- Step 14: Declaration -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 14: Declaration</h4>
                    ${editLink(14)}
                </div>
                <div class="review-dl">
                    <div><dt>Signed By</dt><dd>${getData('decl_name')}</dd></div>
                    <div><dt>Place</dt><dd>${getData('decl_place')}</dd></div>
                    <div><dt>Date</dt><dd>${getData('decl_date')}</dd></div>
                </div>
            </div>
                    ${editLink(4)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Organization</th><th>Duration</th><th>Designation/Proof</th><th>Proof</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#experienceContainer .experience-card')).map(card => {
                                const org = card.querySelector('[name="exp_org[]"]')?.value;
                                if (!org) return '';
                                return `<tr>
                                    <td>${org}</td>
                                    <td>${card.querySelector('.exp-duration-hidden')?.value || '-'}</td>
                                    <td>${card.querySelector('[name="exp_designation[]"]')?.value || '-'}</td>
                                    <td>${getFileStatus('exp_proof[]')}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="4" style="text-align:center; padding:12px; color:var(--ink-light);">No work experience added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 5: Project Experience (Cat 1) -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 5: Project Experience</h4>
                    ${editLink(5)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Project Name</th><th>Client</th><th>Location</th><th>Year</th><th>Role</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#projectContainerCat1 .project-card')).map(card => {
                                const pName = card.querySelector('[name="project_name_cat1[]"]')?.value;
                                if (!pName) return '';
                                return `<tr>
                                    <td>${pName}</td>
                                    <td>${card.querySelector('[name="project_client_cat1[]"]')?.value || '-'}</td>
                                    <td>${card.querySelector('[name="project_location_cat1[]"]')?.value || '-'}</td>
                                    <td>${card.querySelector('[name="project_year_cat1[]"]')?.value || '-'}</td>
                                    <td>${card.querySelector('[name="project_role_cat1[]"]')?.value || '-'}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="5" style="text-align:center; padding:12px; color:var(--ink-light);">No projects added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 6: Upload Reports -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 6: Upload Reports</h4>
                    ${editLink(6)}
                </div>
                <div class="review-dl">
                    <div><dt>Audit Reports Status</dt><dd>${getFileStatus('audit_report')}</dd></div>
                </div>
            </div>

            <!-- Step 7: Training & Certification -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 7: Details of Certification / Training</h4>
                    ${editLink(7)}
                </div>
                <div class="review-table-wrap" style="margin-bottom: 12px;">
                    <p style="font-size: 0.8rem; font-weight: 700; color: var(--accent); margin-bottom: 4px;">Training Records</p>
                    <table class="review-table">
                        <tbody>
                            ${Array.from(document.querySelectorAll('#trainingRowsContainer .step7-grid')).map(grid => {
                                const name = grid.querySelector('[name="training_name[]"]')?.value;
                                if (!name) return '';
                                return `<tr>
                                    <td><strong>${name}</strong> from ${grid.querySelector('[name="training_from[]"]')?.value || '-'} (${grid.querySelector('[name="training_duration[]"]')?.value || '-'} days, ${grid.querySelector('[name="training_year[]"]')?.value || '-'})</td>
                                    <td style="width: 80px;">${getFileStatus('training_proof[]')}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="2" style="text-align:center; padding:8px; color:var(--ink-light);">No training records.</td></tr>'}
                        </tbody>
                    </table>
                </div>
                <div class="review-table-wrap">
                    <p style="font-size: 0.8rem; font-weight: 700; color: var(--accent); margin-bottom: 4px;">Certification Records</p>
                    <table class="review-table">
                        <tbody>
                            ${Array.from(document.querySelectorAll('#certificationRowsContainer .step7-grid')).map(grid => {
                                const name = grid.querySelector('[name="training_name[]"]')?.value;
                                if (!name) return '';
                                return `<tr>
                                    <td><strong>${name}</strong> by ${grid.querySelector('[name="training_from[]"]')?.value || '-'} (${grid.querySelector('[name="training_duration[]"]')?.value || '-'}, ${grid.querySelector('[name="training_year[]"]')?.value || '-'})</td>
                                    <td style="width: 80px;">${getFileStatus('training_proof[]')}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="2" style="text-align:center; padding:8px; color:var(--ink-light);">No certification records.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 8: Membership -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 8: Membership in Professional Bodies</h4>
                    ${editLink(8)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Institution</th><th>Position</th><th>Status</th><th>Year</th><th>Proof</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#membershipContainer .membership-row')).map(row => {
                                const inst = row.querySelector('[name="membership_name[]"]')?.value;
                                if (!inst) return '';
                                return `<tr>
                                    <td>${inst}</td>
                                    <td>${row.querySelector('[name="membership_from[]"]')?.value || '-'}</td>
                                    <td>${row.querySelector('.membership-duration-hidden')?.value || '-'}</td>
                                    <td>${row.querySelector('[name="membership_year[]"]')?.value || '-'}</td>
                                    <td>${getFileStatus('membership_proof[]')}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="5" style="text-align:center; padding:12px; color:var(--ink-light);">No memberships added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 9: Publications -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 9: Paper Published / Presented</h4>
                    ${editLink(9)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Topic</th><th>Venue</th><th>Year</th><th>Link</th><th>Proof</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#publicationContainer .publication-row')).map(row => {
                                const topic = row.querySelector('[name="paper_name[]"]')?.value;
                                if (!topic) return '';
                                return `<tr>
                                    <td>${topic}</td>
                                    <td>${row.querySelector('[name="paper_place[]"]')?.value || '-'}</td>
                                    <td>${row.querySelector('[name="paper_year[]"]')?.value || '-'}</td>
                                    <td style="max-width: 100px; overflow: hidden; text-overflow: ellipsis;">${row.querySelector('[name="paper_role[]"]')?.value || '-'}</td>
                                    <td>${getFileStatus('paper_proof[]')}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="5" style="text-align:center; padding:12px; color:var(--ink-light);">No publications added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 10: Awards -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 10: Awards / Recognition</h4>
                    ${editLink(10)}
                </div>
                <div class="review-table-wrap">
                    <table class="review-table">
                        <thead><tr><th>Award</th><th>By</th><th>Year</th></tr></thead>
                        <tbody>
                            ${Array.from(document.querySelectorAll('#awardTable tbody tr')).map(tr => {
                                const award = tr.querySelector('[name="award_name[]"]')?.value;
                                if (!award) return '';
                                return `<tr>
                                    <td>${award}</td>
                                    <td>${tr.querySelector('[name="award_from[]"]')?.value || '-'}</td>
                                    <td>${tr.querySelector('[name="award_year[]"]')?.value || '-'}</td>
                                </tr>`;
                            }).join('') || '<tr><td colspan="3" style="text-align:center; padding:12px; color:var(--ink-light);">No awards added.</td></tr>'}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Step 11: Software Skills -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 11: Software Skills</h4>
                    ${editLink(11)}
                </div>
                <ul style="margin: 0; padding: 0; list-style: none;">
                    ${Array.from(document.querySelectorAll('#softwareTable tbody tr')).map(tr => {
                        const skill = tr.querySelector('[name="software_skill[]"]')?.value;
                        if (!skill) return '';
                        return `<li style="padding: 6px 12px; background: #f8fafc; border-radius: 6px; margin-bottom: 4px; display: inline-block; margin-right: 4px; font-size: 0.85rem;"><strong>${skill}</strong>: ${tr.querySelector('[name="proficiency_level[]"]')?.value || 'Basic'}</li>`;
                    }).join('') || '<p style="color:var(--ink-light); margin:0;">No special software skills listed.</p>'}
                </ul>
            </div>

            <!-- Step 12: Other Enclosures -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 12: Other Relevant Enclosures</h4>
                    ${editLink(12)}
                </div>
                <div class="review-dl">
                    <div><dt>Description</dt><dd>${document.getElementById('enclosure_desc')?.value || 'Not provided'}</dd></div>
                    <div><dt>File Proof</dt><dd>${getFileStatus('other_enclosure')}</dd></div>
                </div>
            </div>

            <!-- Step 13: Payment Details -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 13: Payment Details</h4>
                    ${editLink(13)}
                </div>
                <div class="review-dl">
                    <div><dt>Amount</dt><dd>â‚¹${getData('payment_amount')}</dd></div>
                    <div><dt>Date</dt><dd>${getData('payment_date')}</dd></div>
                    <div><dt>UTR No</dt><dd>${getData('payment_utr')}</dd></div>
                    <div><dt>Receipt</dt><dd>${getFileStatus('payment_receipt')}</dd></div>
                </div>
            </div>

            <!-- Step 14: Declaration -->
            <div class="review-section">
                <div class="review-header">
                    <h4>Step 14: Declaration by Applicant</h4>
                    ${editLink(14)}
                </div>
                <div class="review-dl">
                    <div><dt>Full Name</dt><dd>${getData('decl_name')}</dd></div>
                    <div><dt>Place</dt><dd>${getData('decl_place')}</dd></div>
                    <div><dt>Date</dt><dd>${getData('decl_date')}</dd></div>
                    <div><dt>Signature</dt><dd>${getFileStatus('signature_file')}</dd></div>
                </div>
            </div>
        `;
        summary.innerHTML = html;
        validateChecklistState(); // Initial button state on review
    }

    function clearChecklist() {
        const cbs = document.querySelectorAll('.checklist-cb');
        cbs.forEach(cb => cb.checked = false);
        validateChecklistState();
    }

    function validateFileUpload(input) {
        const file = input.files[0];
        if (!file) return;

        const fileName = file.name.toLowerCase();
        const fileSize = file.size;
        const isImage = /\.(jpg|jpeg|png)$/.test(fileName);
        const isPdf = /\.pdf$/.test(fileName);

        let isValid = true;
        let errorMsg = "";
        let limitMsg = "";

        if (isImage) {
            if (fileSize > 2 * 1024 * 1024) {
                isValid = false;
                errorMsg = "Image must be less than 2 MB";
            }
            limitMsg = "Max size: 2 MB (Image)";
        } else if (isPdf) {
            if (fileSize > 10 * 1024 * 1024) {
                isValid = false;
                errorMsg = "PDF must be less than 10 MB";
            }
            limitMsg = "Max size: 10 MB (PDF)";
        } else {
            if (fileSize > 10 * 1024 * 1024) {
                isValid = false;
                errorMsg = "File must be less than 10 MB";
            }
            limitMsg = "Max size: 10 MB";
        }

        let hint = input.parentNode.querySelector('.file-validation-hint, .identity-hint');
        if (!hint) {
            hint = document.createElement('div');
            hint.className = 'file-validation-hint';
            hint.style.fontSize = '0.75rem';
            hint.style.marginTop = '4px';
            input.parentNode.appendChild(hint);
        }

        if (!isValid) {
            showToast(errorMsg, "error");
            input.value = "";
            hint.innerHTML = `<span style="color: #c53030; font-weight: 600;">âš ï¸ ${errorMsg}</span>`;
        } else {
            hint.innerHTML = `<span style="color: #2f855a; font-weight: 600;">âœ“ ${limitMsg}</span>`;
        }
    }

    function previewPhoto(event) {
        const file = event.target.files[0];
        const preview = document.getElementById('photoPreview');
        if (file && preview) {
            const reader = new FileReader();
            reader.onload = e => { preview.src = e.target.result; preview.style.display = 'block'; };
            reader.readAsDataURL(file);
        }
    }

    function toggleGovIdUpload() {
        const type = document.getElementById('govIdType')?.value;
        const otherContainer = document.getElementById('otherGovIdContainer');
        const otherInput = document.getElementById('otherGovIdType');
        const label = document.getElementById('govIdLabel');
        
        if (otherContainer && otherInput) {
            if (type === 'other') {
                otherContainer.style.display = 'block';
                otherInput.required = true;
            } else {
                otherContainer.style.display = 'none';
                otherInput.required = false;
                otherInput.value = '';
            }
        }
        
        if (label) {
            label.textContent = (type === 'aadhaar' ? 'Aadhaar' : (type === 'pan' ? 'PAN' : (type === 'passport' ? 'Passport' : 'ID'))) + ' Number *';
        }
    }

    document.addEventListener('DOMContentLoaded', () => {
        updateUI();
        
        // Setup blur validation for meaningless text globally
        document.querySelectorAll('input[type="text"], textarea').forEach(input => {
            const skipNames = ['gov_id_number', 'payment_utr', 'mobile', 'alt_mobile', 'email', 'total_experience', 'payment_amount', 'address_perm', 'address_corr'];
            if (skipNames.includes(input.name)) return;
            
            input.addEventListener('blur', function() {
                if (this.value && this.value.trim() !== '') {
                    const isDesc = this.tagName === 'TEXTAREA' || this.name === 'enclosure_desc';
                    if (typeof isMeaningfulText === 'function' && !isMeaningfulText(this.value, isDesc)) {
                        this.style.borderColor = 'var(--warn)';
                        if (!this.parentNode.querySelector('.meaningful-error')) {
                            const err = document.createElement('div');
                            err.className = 'meaningful-error file-validation-hint';
                            err.style.color = '#c53030';
                            err.style.fontSize = '0.75rem';
                            err.style.marginTop = '4px';
                            err.style.fontWeight = '600';
                            err.innerText = "âš ï¸ " + (isDesc ? "Please enter a meaningful description." : "Please enter meaningful information.");
                            this.parentNode.appendChild(err);
                        }
                    } else {
                        this.style.borderColor = '';
                        if (this.parentNode.querySelector('.meaningful-error')) {
                            this.parentNode.querySelector('.meaningful-error').remove();
                        }
                    }
                } else if (!this.value || this.value.trim() === '') {
                     // Empty values clear meaningful errors, standard 'required' logic applies natively
                     this.style.borderColor = '';
                     if (this.parentNode.querySelector('.meaningful-error')) {
                          this.parentNode.querySelector('.meaningful-error').remove();
                     }
                }
            });
        });
        
        // Setup blur validation specifically for Address inputs
        document.querySelectorAll('textarea[name="address_perm"], textarea[name="address_corr"]').forEach(input => {
            input.addEventListener('blur', function() {
                const val = this.value ? this.value.trim() : '';
                if (val !== '' && !/^[a-zA-Z0-9\s,.\-/#]{3,}$/.test(val)) {
                     this.style.borderColor = 'var(--warn)';
                     if (!this.parentNode.querySelector('.address-error')) {
                         const err = document.createElement('div');
                         err.className = 'address-error file-validation-hint';
                         err.style.color = '#c53030';
                         err.style.fontSize = '0.75rem';
                         err.style.marginTop = '4px';
                         err.style.fontWeight = '600';
                         err.innerText = "âš ï¸ Address must be at least 3 characters.";
                         this.parentNode.appendChild(err);
                     }
                } else {
                     this.style.borderColor = '';
                     if (this.parentNode.querySelector('.address-error')) {
                         this.parentNode.querySelector('.address-error').remove();
                     }
                }
            });
        });
        
        // Sidebar Navigation Handling
        document.querySelectorAll('.nav-item').forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const target = parseInt(item.getAttribute('data-step'));
                
                if (target <= maxReachedStep + 1) {
                    if (target > currentStep && !validateSection(currentStep)) return;
                    currentStep = target;
                    updateUI();
                } else {
                    showToast("Please complete previous steps first.", "error");
                }
            });
        });

        // Checklist Change Listener
        document.getElementById('checklistBody').addEventListener('change', (e) => {
            if (e.target.classList.contains('checklist-cb')) {
                validateChecklistState();
            }
        });

        // Handle Final Submit Button Binding
        const submitBtn = document.getElementById("submitFinalApplicationBtn");
        if (submitBtn) {
            submitBtn.onclick = submitFinalApplication;
        }

        // Trigger Document Requirement Modal only after successful login/signup
        const shouldShow = localStorage.getItem('showDocPopup');
        const hidePermanently = localStorage.getItem('hideDocPopupForever');

        if (shouldShow === 'true' && hidePermanently !== 'true') {
            setTimeout(() => {
                const docModal = document.getElementById('docReqModal');
                if (docModal) {
                    docModal.classList.add('active');
                    localStorage.removeItem('showDocPopup'); // Ensure it only shows once per login
                }
            }, 800);
        }
        // --- CANDIDATE PHOTOGRAPH FIX ---
        const uploadBox = document.getElementById("candidatePhotoUploadBox");
        const fileInput = document.getElementById("candidatePhotoInput");

        if (uploadBox && fileInput) {
            uploadBox.addEventListener("click", function () {
                fileInput.click();
            });

            fileInput.addEventListener("change", function (event) {
                const file = event.target.files[0];
                if (!file) return;

                const allowedTypes = ["image/jpeg", "image/png"];
                const preview = document.getElementById("profilePreviewImage");
                const actions = document.getElementById("photoActions");
                const subtitle = document.getElementById("uploadSubtitle");

                if (!allowedTypes.includes(file.type)) {
                    alert("Only JPG and PNG files are allowed.");
                    fileInput.value = "";
                    return;
                }

                if (file.size > 2 * 1024 * 1024) {
                    alert("File size must be less than 2MB.");
                    fileInput.value = "";
                    return;
                }

                const reader = new FileReader();
                reader.onload = function (e) {
                    if (preview) preview.src = e.target.result;
                    if (subtitle) subtitle.style.display = 'none';
                    if (actions) actions.style.display = 'flex';
                };
                reader.readAsDataURL(file);
            });
        }
    });

    function toggleAddressSync() {
        const isChecked = document.getElementById('sameAddress').checked;
        const corr = document.getElementById('address_corr');
        const perm = document.getElementById('address_perm');
        
        if (isChecked) {
            corr.value = perm.value;
            corr.readOnly = true;
            corr.style.background = 'var(--bg)';
            corr.style.cursor = 'not-allowed';
        } else {
            corr.value = '';
            corr.readOnly = false;
            corr.style.background = '#fff';
            corr.style.cursor = 'text';
        }
    }

    function syncAddress() {
        const isChecked = document.getElementById('sameAddress').checked;
        if (isChecked) {
            document.getElementById('address_corr').value = document.getElementById('address_perm').value;
        }
    }

    function removePhoto() {
        const input = document.getElementById('candidatePhotoInput');
        const preview = document.getElementById('profilePreviewImage');
        const actions = document.getElementById('photoActions');
        const subtitle = document.getElementById('uploadSubtitle');

        if (input) input.value = '';
        if (preview) preview.src = 'https://via.placeholder.com/150?text=Profile';
        if (actions) actions.style.display = 'none';
        if (subtitle) subtitle.style.display = 'block';
    }

    function closeDocModal() {
        const modal = document.getElementById('docReqModal');
        const hideForever = document.getElementById('chkHideForever').checked;
        if (hideForever) {
            localStorage.setItem('hideDocPopupForever', 'true');
        }
        modal.classList.remove('active');
    }

    function closeDocModal() {
        const modal = document.getElementById('docReqModal');
        const hideForever = document.getElementById('chkHideForever').checked;
        if (hideForever) {
            localStorage.setItem('hideDocPopupForever', 'true');
        }
        modal.classList.remove('active');
    }

