
// Administrador de gráficas del dashboard

// Gráfica circular de ingresos
export function createRevenueChart() {
    const canvas = document.getElementById("revenueChart");

    if (!canvas) return;

    const existingChart = Chart.getChart(canvas);
    if (existingChart) existingChart.destroy();

    new Chart(canvas, {
        type: "doughnut",
        data: {
            labels: ["Otros ingresos", "Consultas", "Cirugías"],
            datasets: [{
                data: [45000, 85000, 400000],
                backgroundColor: ["#9E9A9B", "#C67C8E", "#9E6E79"],
                borderWidth: 0,
                hoverOffset: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: "72%",
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return context.label + ": RD$ " +
                                context.raw.toLocaleString("es-DO");
                        }
                    }
                }
            }
        }
    });
}

// Gráfica de distribución de pacientes
export function createPatientsChart() {
    const canvas = document.getElementById("patientsChart");

    if (!canvas) return;

    const existingChart = Chart.getChart(canvas);
    if (existingChart) existingChart.destroy();

    new Chart(canvas, {
        type: "doughnut",
        data: {
            labels: ["Masculino", "Femenino"],
            datasets: [{
                data: [35, 65],
                backgroundColor: ["#9E9A9B", "#C67C8E"],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: "65%",
            plugins: {
                legend: { display: false }
            }
        }
    });
}

// Gráfica de actividad del consultorio
export function createActivityChart() {
    const canvas = document.getElementById("activityChart");

    if (!canvas) return;

    const existingChart = Chart.getChart(canvas);
    if (existingChart) existingChart.destroy();

    new Chart(canvas, {
        type: "line",
        data: {
            labels: ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"],
            datasets: [
                {
                    label: "En línea",
                    data: [12, 19, 15, 22, 18, 25, 20],
                    borderColor: "#9E6E79",
                    backgroundColor: "rgba(158, 110, 121, 0.12)",
                    fill: true,
                    tension: 0.4
                },
                {
                    label: "Fuera de línea",
                    data: [4, 6, 5, 7, 4, 8, 6],
                    borderColor: "#9E9A9B",
                    backgroundColor: "transparent",
                    tension: 0.4
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: { color: "#EEEEEE" }
                },
                x: {
                    grid: { display: false }
                }
            }
        }
    });
}
