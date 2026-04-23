document.addEventListener('DOMContentLoaded', () => {
    const flagForm = document.getElementById('flagForm');
    const submitFlagBtn = document.getElementById('submitFlagBtn');
    const flagBtn = document.getElementById('flagBtn');
    const flagMessage = document.getElementById('flagMessage');
    const flagModalEl = document.getElementById('flagModal');
    let flagModal = null;

    if (flagModalEl) {
        flagModal = new bootstrap.Modal(flagModalEl);
    }

    if (submitFlagBtn && flagForm) {
        submitFlagBtn.addEventListener('click', async () => {
            const formData = new FormData(flagForm);
            const reportId = formData.get('reportId');
            const category = formData.get('category');

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

                    // Disable the original flag button
                    if (flagBtn) {
                        flagBtn.disabled = true;
                        flagBtn.textContent = 'Already Flagged';
                        flagBtn.classList.remove('btn-outline-danger');
                        flagBtn.classList.add('btn-secondary');
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
