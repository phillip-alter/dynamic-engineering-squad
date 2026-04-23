document.addEventListener('DOMContentLoaded', () => {
    const flagForm = document.getElementById('flagForm');
    const submitFlagBtn = document.getElementById('submitFlagBtn');
    const flagMessage = document.getElementById('flagMessage');
    const flagModalEl = document.getElementById('flagModal');
    const flagReportIdInput = document.getElementById('flagReportId');
    let flagModal = null;

    if (flagModalEl) {
        flagModal = new bootstrap.Modal(flagModalEl);
        
        // When modal is hidden, reset the form and message
        flagModalEl.addEventListener('hidden.bs.modal', () => {
            flagForm.reset();
            flagMessage.classList.add('d-none');
            flagMessage.textContent = '';
            submitFlagBtn.disabled = false;
            submitFlagBtn.textContent = 'Submit Report';
        });
    }

    // Handle the "Flag" button click on Details page
    const detailsFlagBtn = document.getElementById('flagBtn');
    if (detailsFlagBtn) {
        detailsFlagBtn.addEventListener('click', () => {
            const reportId = detailsFlagBtn.getAttribute('data-report-id');
            if (flagReportIdInput) {
                flagReportIdInput.value = reportId;
            }
        });
    }

    // Handle the "Flag" button click in Latest Reports modal
    const modalFlagBtn = document.getElementById('modalFlagBtn');
    if (modalFlagBtn) {
        modalFlagBtn.addEventListener('click', () => {
            const reportId = modalFlagBtn.getAttribute('data-report-id');
            if (flagReportIdInput) {
                flagReportIdInput.value = reportId;
            }
            if (flagModal) {
                flagModal.show();
            }
        });
    }

    if (submitFlagBtn && flagForm) {
        submitFlagBtn.addEventListener('click', async () => {
            const reportId = flagReportIdInput.value;
            const category = flagForm.querySelector('input[name="category"]:checked').value;

            if (!reportId) return;

            submitFlagBtn.disabled = true;
            submitFlagBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Submitting...';

            try {
                const response = await fetch('/Flag/Create', {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value,
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: new URLSearchParams({
                        reportId: reportId,
                        category: category
                    })
                });

                const result = await response.json();

                if (result.success) {
                    flagMessage.textContent = result.message;
                    flagMessage.classList.remove('d-none', 'alert-danger');
                    flagMessage.classList.add('alert-success');

                    // Update UI buttons if they exist for this report
                    const buttonsToUpdate = document.querySelectorAll(`button[data-report-id="${reportId}"][id*="flagBtn"], button[id="modalFlagBtn"]`);
                    buttonsToUpdate.forEach(btn => {
                        btn.disabled = true;
                        btn.textContent = 'Already Flagged';
                        btn.classList.remove('btn-outline-danger');
                        btn.classList.add('btn-secondary');
                    });

                    // Special case for detailsFlagBtn if it doesn't have data-report-id (it usually does)
                    if (detailsFlagBtn && !detailsFlagBtn.getAttribute('data-report-id')) {
                         detailsFlagBtn.disabled = true;
                         detailsFlagBtn.textContent = 'Already Flagged';
                         detailsFlagBtn.classList.remove('btn-outline-danger');
                         detailsFlagBtn.classList.add('btn-secondary');
                    }

                    // Close modal after a short delay
                    setTimeout(() => {
                        if (flagModal) {
                            flagModal.hide();
                        }
                    }, 2000);
                } else {
                    flagMessage.textContent = result.message || 'An error occurred.';
                    flagMessage.classList.remove('d-none', 'alert-success');
                    flagMessage.classList.add('alert-danger');
                    submitFlagBtn.disabled = false;
                    submitFlagBtn.textContent = 'Submit Report';
                }
            } catch (error) {
                console.error('Error flagging report:', error);
                flagMessage.textContent = 'A network error occurred. Please try again.';
                flagMessage.classList.remove('d-none', 'alert-success');
                flagMessage.classList.add('alert-danger');
                submitFlagBtn.disabled = false;
                submitFlagBtn.textContent = 'Submit Report';
            }
        });
    }
});
